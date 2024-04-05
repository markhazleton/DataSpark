using Microsoft.Data.Sqlite;
using System.IO;
using System.Text;

namespace Sql2Csv;

/// <summary>
/// Represents a class that exports tables from a Sqlite database to CSV files.
/// </summary>
public class SqliteToCsvExporter(DbConfiguration db)
{
    /// <summary>
    /// Exports all tables from the Sqlite database to CSV files.
    /// </summary>
    public void ExportTablesToCsv()
    {
        using var connection = new SqliteConnection(db.ConnectionString);
        connection.Open();
        var tables = GetTableNames(connection);
        foreach (var table in tables)
        {
            ExportTableToCsv(connection, table);
        }
    }

    /// <summary>
    /// Retrieves the names of all tables in the Sqlite database.
    /// </summary>
    /// <param name="connection">The Sqlite connection.</param>
    /// <returns>An array of table names.</returns>
    private static string[] GetTableNames(SqliteConnection connection)
    {
        var tables = new System.Collections.Generic.List<string>();
        var query = "SELECT name FROM Sqlite_master WHERE type='table';";
        using (var command = new SqliteCommand(query, connection))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                tables.Add(reader["name"].ToString());
            }
        }
        return tables.ToArray();
    }

    /// <summary>
    /// Exports a table from the Sqlite database to a CSV file.
    /// </summary>
    /// <param name="connection">The Sqlite connection.</param>
    /// <param name="tableName">The name of the table to export.</param>
    private void ExportTableToCsv(SqliteConnection connection, string tableName)
    {
        // create folder with database name
        Directory.CreateDirectory(db.DatabaseName);

        var csvFilePath = $"{db.DatabaseName}\\{tableName}_extract.csv";
        using var writer = new StreamWriter(csvFilePath, false, Encoding.UTF8);
        using var command = new SqliteCommand($"SELECT * FROM {tableName};", connection);
        using var reader = command.ExecuteReader();
        // Write column headers
        for (int i = 0; i < reader.FieldCount; i++)
        {
            writer.Write($"\"{reader.GetName(i)}\"");
            if (i < reader.FieldCount - 1)
                writer.Write(",");
        }
        writer.WriteLine();

        // Write rows
        while (reader.Read())
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                writer.Write($"\"{reader[i].ToString().Replace("\"", "\"\"")}\""); // Escape quotes
                if (i < reader.FieldCount - 1)
                    writer.Write(",");
            }
            writer.WriteLine();
        }
    }
}
