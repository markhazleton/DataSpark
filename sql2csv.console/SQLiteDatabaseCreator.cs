using Microsoft.Data.Sqlite;
using System;

namespace Sql2Csv;

/// <summary>
/// A static class that creates a Sqlite database and table, and performs operations on the table.
/// </summary>
public class SqliteDatabaseCreator
{
    private string _connectionString;

    /// <summary>
    /// Creates a Sqlite database and table if they don't exist, and inserts initial data into the table.
    /// </summary>
    /// <returns>The connection string of the created database.</returns>
    public SqliteDatabaseCreator(string _rootPath)
    {
        try
        {
            _connectionString = $"Data Source={_rootPath}\\data\\test.db";

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Check if the 'test' table already exists
            var checkTableExistsQuery = "SELECT name FROM Sqlite_master WHERE type='table' AND name='test';";
            bool tableExists;
            using (var command = new SqliteCommand(checkTableExistsQuery, connection))
            {
                using var reader = command.ExecuteReader();
                tableExists = reader.HasRows;  // true if table exists
            }

            if (!tableExists)
            {
                // Create the test table if it doesn't exist
                var createTableQuery = @"
                        CREATE TABLE test (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            name TEXT NOT NULL
                        )";
                using (var command = new SqliteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }

                // Insert records into the test table
                var insertDataQuery = @"
                        INSERT INTO test (name) VALUES ('John Doe');
                        INSERT INTO test (name) VALUES ('Jane Doe');
                        INSERT INTO test (name) VALUES ('Jim Beam');
                    ";
                using (var command = new SqliteCommand(insertDataQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
            else
            {
                Console.WriteLine("Table 'test' already exists. Skipping table creation and initial data insertion.");
            }

            // Select data from the test table
            var selectDataQuery = "SELECT * FROM test";
            using (var command = new SqliteCommand(selectDataQuery, connection))
            {
                using var reader = command.ExecuteReader();
                Console.WriteLine("id\tname");
                while (reader.Read())
                {
                    Console.WriteLine($"{reader.GetInt32(0)}\t{reader.GetString(1)}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    public string ConnectionString => _connectionString;
}

