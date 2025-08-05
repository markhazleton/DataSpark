using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Sql2Csv.Tests.Infrastructure;

/// <summary>
/// Base class for tests that require a SQLite database.
/// </summary>
public abstract class DatabaseTestBase
{
    protected string TestDatabasePath { get; private set; } = null!;
    protected string ConnectionString { get; private set; } = null!;
    private readonly List<SqliteConnection> _openConnections = new();

    [TestInitialize]
    public virtual async Task TestInitialize()
    {
        TestDatabasePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        ConnectionString = $"Data Source={TestDatabasePath}";

        await CreateTestDatabaseAsync();
    }

    [TestCleanup]
    public virtual void TestCleanup()
    {
        // Dispose all tracked connections
        foreach (var connection in _openConnections)
        {
            try
            {
                if (connection.State != System.Data.ConnectionState.Closed)
                {
                    connection.Close();
                }
                connection.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }
        _openConnections.Clear();

        // Clear connection pools
        SqliteConnection.ClearAllPools();

        // Force garbage collection to release any pending database connections
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Attempt to delete with retry logic
        DeleteFileWithRetry(TestDatabasePath, maxRetries: 5, delayMs: 200);
    }

    /// <summary>
    /// Create a tracked SQLite connection that will be automatically disposed.
    /// </summary>
    protected SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(ConnectionString);
        _openConnections.Add(connection);
        return connection;
    }

    private static void DeleteFileWithRetry(string filePath, int maxRetries, int delayMs)
    {
        if (!File.Exists(filePath))
            return;

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                File.Delete(filePath);
                return;
            }
            catch (IOException) when (i < maxRetries - 1)
            {
                Thread.Sleep(delayMs);
            }
        }

        // If we still can't delete, just ignore it to avoid test failures
        try
        {
            File.Delete(filePath);
        }
        catch (IOException)
        {
            // Ignore final cleanup failure
        }
    }

    protected virtual async Task CreateTestDatabaseAsync()
    {
        using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        // Create test tables
        var createUsersTable = @"
            CREATE TABLE Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Email TEXT,
                Age INTEGER,
                CreatedDate TEXT DEFAULT CURRENT_TIMESTAMP
            )";

        var createOrdersTable = @"
            CREATE TABLE Orders (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER,
                Amount REAL,
                OrderDate TEXT DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (UserId) REFERENCES Users(Id)
            )";

        var createProductsTable = @"
            CREATE TABLE Products (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Price REAL,
                Description TEXT
            )";

        using var command = connection.CreateCommand();

        command.CommandText = createUsersTable;
        await command.ExecuteNonQueryAsync();

        command.CommandText = createOrdersTable;
        await command.ExecuteNonQueryAsync();

        command.CommandText = createProductsTable;
        await command.ExecuteNonQueryAsync();

        // Insert test data
        await InsertTestDataAsync(connection);
    }

    protected virtual async Task InsertTestDataAsync(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();

        // Insert users
        command.CommandText = @"
            INSERT INTO Users (Name, Email, Age) VALUES 
            ('John Doe', 'john@example.com', 30),
            ('Jane Smith', 'jane@example.com', 25),
            ('Bob Johnson', 'bob@example.com', 35)";
        await command.ExecuteNonQueryAsync();

        // Insert orders
        command.CommandText = @"
            INSERT INTO Orders (UserId, Amount) VALUES 
            (1, 99.99),
            (1, 149.50),
            (2, 75.25),
            (3, 200.00)";
        await command.ExecuteNonQueryAsync();

        // Insert products
        command.CommandText = @"
            INSERT INTO Products (Name, Price, Description) VALUES 
            ('Widget A', 19.99, 'A useful widget'),
            ('Widget B', 29.99, 'An even more useful widget'),
            ('Gadget X', 49.99, 'The ultimate gadget')";
        await command.ExecuteNonQueryAsync();
    }

    protected async Task<int> GetRowCountAsync(string tableName)
    {
        using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {tableName}";

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    protected async Task<bool> TableExistsAsync(string tableName)
    {
        using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=@tableName";
        command.Parameters.AddWithValue("@tableName", tableName);

        var result = await command.ExecuteScalarAsync();
        return result != null;
    }
}
