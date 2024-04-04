using Microsoft.Data.Sqlite;
using System;

namespace Sql2Csv;

public static class SqliteDatabaseCreator
{
    const string STR_Db = "Data Source=mydatabase.db;";
    public static string CreateDatabaseAndTable()
    {
        // Create a connection to the database
        using (var connection = new SqliteConnection(STR_Db))
        {
            connection.Open();

            // Create a test table
            var createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS test (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL
                    )";

            using (var command = new SqliteCommand(createTableQuery, connection))
            {
                command.ExecuteNonQuery();
            }

            // Insert a few records into the test table
            var insertDataQuery = @"
                    INSERT INTO test (name) VALUES ('John Doe');
                    INSERT INTO test (name) VALUES ('Jane Doe');
                    INSERT INTO test (name) VALUES ('Jim Beam');
                ";
            using (var command = new SqliteCommand(insertDataQuery, connection))
            {
                command.ExecuteNonQuery();
            }
            connection.Close();
        }
        Console.WriteLine($"Database '{STR_Db}' created with a 'test' table and sample data inserted.");
        return $"Data Source={STR_Db}";
    }
}

