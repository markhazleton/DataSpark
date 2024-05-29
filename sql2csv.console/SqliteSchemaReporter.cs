using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace Sql2Csv;

public class SqliteSchemaReporter
{
    public static void ReportTablesAndColumns(string SqliteConnectionString)
    {
        using var connection = new SqliteConnection(SqliteConnectionString);
        connection.Open();
        foreach (var table in GetTableNames(connection))
        {
            Console.WriteLine($"Table: {table}");
            ReportColumnsForTable(connection, table);
            Console.WriteLine(); // Extra line for readability
        }
    }
    private static string[] GetTableNames(SqliteConnection connection)
    {
        var tables = new List<string>();
        var query = "SELECT name FROM Sqlite_master WHERE type='table' AND name NOT LIKE 'Sqlite_%';";
        using (var command = new SqliteCommand(query, connection))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                tables.Add(reader["name"].ToString());
            }
        }
        return [.. tables];
    }
    private static void ReportColumnsForTable(SqliteConnection connection, string tableName)
    {
        using var command = new SqliteCommand($"PRAGMA table_info({tableName});", connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var columnName = reader["name"].ToString();
            var dataType = reader["type"].ToString();
            Console.WriteLine($"\tColumn: {columnName}, Type: {dataType}");
        }
    }
}
