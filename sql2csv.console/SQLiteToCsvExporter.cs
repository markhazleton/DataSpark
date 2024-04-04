using Microsoft.Data.Sqlite;
using System.IO;
using System.Text;

namespace Sql2Csv;

public class SqliteToCsvExporter(string sqliteFilename)
{
    public void ExportTablesToCsv()
    {
        using (var connection = new SqliteConnection($"Data Source={sqliteFilename};"))
        {
            connection.Open();

            var tables = GetTableNames(connection);

            foreach (var table in tables)
            {
                ExportTableToCsv(connection, table);
            }
        }
    }

    private static string[] GetTableNames(SqliteConnection connection)
    {
        var tables = new System.Collections.Generic.List<string>();
        var query = "SELECT name FROM sqlite_master WHERE type='table';";
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

    private static void ExportTableToCsv(SqliteConnection connection, string tableName)
    {
        var csvFilePath = $"{tableName}_extract.csv";
        using (var writer = new StreamWriter(csvFilePath, false, Encoding.UTF8))
        using (var command = new SqliteCommand($"SELECT * FROM {tableName};", connection))
        using (var reader = command.ExecuteReader())
        {
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
}
