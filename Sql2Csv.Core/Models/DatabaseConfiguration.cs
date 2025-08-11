namespace Sql2Csv.Core.Models;

/// <summary>
/// Represents the configuration for a database connection.
/// </summary>
public sealed class DatabaseConfiguration
{
    /// <summary>
    /// Gets or sets the name of the database.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database type.
    /// </summary>
    public DatabaseType Type { get; set; } = DatabaseType.SQLite;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseConfiguration"/> class.
    /// </summary>
    public DatabaseConfiguration()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseConfiguration"/> class.
    /// </summary>
    /// <param name="name">The database name.</param>
    /// <param name="connectionString">The connection string.</param>
    /// <param name="type">The database type.</param>
    public DatabaseConfiguration(string name, string connectionString, DatabaseType type = DatabaseType.SQLite)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        Type = type;
    }

    /// <summary>
    /// Creates a database configuration from a SQLite file path.
    /// </summary>
    /// <param name="filePath">The SQLite file path.</param>
    /// <returns>A new database configuration.</returns>
    public static DatabaseConfiguration FromSqliteFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

    // Normalize path separators so Windows-style paths (with backslashes) work on Unix runners.
    // On Unix, Path.GetFileNameWithoutExtension does not treat '\\' as a directory separator,
    // causing it to return the entire path. We replace both separators with the current platform's
    // separator before extracting the filename.
    var normalized = filePath.Replace('\\', Path.DirectorySeparatorChar)
                 .Replace('/', Path.DirectorySeparatorChar);
    var name = Path.GetFileNameWithoutExtension(normalized);
        var connectionString = $"Data Source={filePath}";

        return new DatabaseConfiguration(name, connectionString, DatabaseType.SQLite);
    }
}

/// <summary>
/// Represents the type of database.
/// </summary>
public enum DatabaseType
{
    /// <summary>
    /// SQLite database.
    /// </summary>
    SQLite,

    /// <summary>
    /// SQL Server database.
    /// </summary>
    SqlServer
}
