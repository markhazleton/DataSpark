using Microsoft.Data.Sqlite;
using System.IO;
using System.Linq;
using System.Text;

namespace Sql2Csv;

public class SQLiteCodeGenerator
{

    private static string ConvertSQLiteTypeToCSharpType(string SQLiteType)
    {
        return SQLiteType.ToUpper() switch
        {
            "INTEGER" => "int",
            "TEXT" => "string",
            "BLOB" => "byte[]",
            "REAL" => "double",
            "NUMERIC" => "decimal",
            _ => "dynamic",// Fallback for types not directly mapped
        };
    }

    private static string GenerateClassContent(string tableName, System.Collections.Generic.Dictionary<string, string> columns)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine();
        sb.AppendLine("namespace Sql2CSV.Generated;");
        sb.AppendLine();
        sb.AppendLine($"public class {tableName}DTO");
        sb.AppendLine("{");

        foreach (var column in columns)
        {
            sb.AppendLine($"    public {column.Value} {column.Key} {{ get; set; }}");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }
    private static string GenerateServiceContent(string tableName, System.Collections.Generic.Dictionary<string, string> columns)
    {
        var sb = new StringBuilder();
        var dtoName = $"{tableName}DTO";

        sb.AppendLine("using System;");
        sb.AppendLine("using System.Data.Sqlite;");
        sb.AppendLine("using System.Collections.Generic;"); // For read operation returning a list
        sb.AppendLine();
        sb.AppendLine("namespace Sql2CSV.Generated;");
        sb.AppendLine();
        sb.AppendLine($"public class {tableName}Service");
        sb.AppendLine("{");
        sb.AppendLine($"    private readonly string _connectionString = \"Data Source=your_database_path.db;Version=3;\";"); // Placeholder connection string
        sb.AppendLine();

        // Create method
        sb.AppendLine($"    public void Create({dtoName} dto)");
        sb.AppendLine("    {");
        sb.AppendLine("        using (var connection = new SqliteConnection(_connectionString))");
        sb.AppendLine("        {");
        sb.AppendLine($"            var command = new SqliteCommand(\"INSERT INTO {tableName} ({string.Join(", ", columns.Keys)}) VALUES ({string.Join(", ", columns.Keys.Select(k => $"@{k.ToLower()}"))})\", connection);");
        foreach (var column in columns)
        {
            sb.AppendLine($"            command.Parameters.AddWithValue(\"@{column.Key.ToLower()}\", dto.{column.Key});");
        }
        sb.AppendLine("            connection.Open();");
        sb.AppendLine("            command.ExecuteNonQuery();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Read method
        sb.AppendLine($"    public List<{dtoName}> Read()");
        sb.AppendLine("    {");
        sb.AppendLine($"        var list = new List<{dtoName}>();");
        sb.AppendLine("        using (var connection = new SqliteConnection(_connectionString))");
        sb.AppendLine("        {");
        sb.AppendLine($"            var command = new SqliteCommand(\"SELECT * FROM {tableName}\", connection);");
        sb.AppendLine("            connection.Open();");
        sb.AppendLine("            using (var reader = command.ExecuteReader())");
        sb.AppendLine("            {");
        sb.AppendLine("                while (reader.Read())");
        sb.AppendLine("                {");
        sb.AppendLine($"                    var dto = new {dtoName}();");
        foreach (var column in columns)
        {
            sb.AppendLine($"                    dto.{column.Key} = reader[\"{column.Key}\"].ToString();"); // Type conversion might be required here
        }
        sb.AppendLine("                    list.Add(dto);");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine($"        return list;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Update method - assuming there's an 'id' column for simplicity
        sb.AppendLine($"    public void Update({dtoName} dto)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Implement the update logic here, similar to Create method");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Delete method - assuming there's an 'id' column for simplicity
        sb.AppendLine($"    public void Delete(int id)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Implement the delete logic here, using the id to identify the record");
        sb.AppendLine("    }");

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static System.Collections.Generic.Dictionary<string, string> GetColumnDefinitions(SqliteConnection connection, string tableName)
    {
        var columns = new System.Collections.Generic.Dictionary<string, string>();
        using (var command = new SqliteCommand($"PRAGMA table_info({tableName});", connection))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                var columnName = reader["name"].ToString();
                var dataType = ConvertSQLiteTypeToCSharpType(reader["type"].ToString());
                columns.Add(columnName, dataType);
            }
        }

        return columns;
    }

    private static string[] GetTableNames(SqliteConnection connection)
    {
        var tables = new System.Collections.Generic.List<string>();
        var query = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";
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

    private static void WriteToFile(string content, string fileName, string directory)
    {
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(Path.Combine(directory, fileName), content);
    }

    public static void GenerateDtoClasses(string sqliteConnectionString, string outputDirectory)
    {
        using var connection = new SqliteConnection(sqliteConnectionString);
        connection.Open();

        var tables = GetTableNames(connection);

        foreach (var table in tables)
        {
            var columns = GetColumnDefinitions(connection, table);
            var classContent = GenerateClassContent(table, columns);
            WriteToFile(classContent, $"{table}DTO.cs", outputDirectory);
        }
        foreach (var table in tables)
        {
            var columns = GetColumnDefinitions(connection, table);
            var serviceContent = GenerateServiceContent(table, columns);
            WriteToFile(serviceContent, $"{table}Service.cs", outputDirectory);
        }
    }
}
