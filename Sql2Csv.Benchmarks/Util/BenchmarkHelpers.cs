using Microsoft.Data.Sqlite;
using Sql2Csv.Core.Models;
using System.Text;

namespace Sql2Csv.Benchmarks.Util;

internal static class BenchmarkHelpers
{
    public static string CreateTempDirectory(string prefix)
    {
        var path = Path.Combine(Path.GetTempPath(), $"sql2csv_bench_{prefix}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    public static DatabaseConfiguration CreateDatabase(int rowCount, int columnCount = 5)
    {
        var filePath = Path.Combine(CreateTempDirectory("db"), $"data_{rowCount}.db");
        var cs = new SqliteConnectionStringBuilder { DataSource = filePath }.ToString();
        using var conn = new SqliteConnection(cs);
        conn.Open();
        using (var cmd = conn.CreateCommand())
        {
            var sb = new StringBuilder();
            sb.Append("CREATE TABLE Data (Id INTEGER PRIMARY KEY AUTOINCREMENT");
            for (int i = 0; i < columnCount - 1; i++)
            {
                sb.Append($", Col{i} TEXT");
            }
            sb.Append(");");
            cmd.CommandText = sb.ToString();
            cmd.ExecuteNonQuery();
        }
        using (var tx = conn.BeginTransaction())
        using (var insert = conn.CreateCommand())
        {
            insert.Transaction = tx;
            var columnNames = Enumerable.Range(0, columnCount - 1).Select(i => $"Col{i}").ToArray();
            var paramNames = columnNames.Select((_, i) => $"@p{i}").ToArray();
            insert.CommandText = $"INSERT INTO Data ({string.Join(",", columnNames)}) VALUES ({string.Join(",", paramNames)});";
            for (int i = 0; i < paramNames.Length; i++)
            {
                insert.Parameters.Add(new SqliteParameter(paramNames[i], System.Data.DbType.String));
            }
            for (int r = 0; r < rowCount; r++)
            {
                for (int c = 0; c < paramNames.Length; c++)
                {
                    insert.Parameters[c].Value = $"row{r}_value{c}";
                }
                insert.ExecuteNonQuery();
            }
            tx.Commit();
        }
        return DatabaseConfiguration.FromSqliteFile(filePath);
    }

    public static string CreateDatabaseAt(string directory, string fileName, int rowCount)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, fileName);
        var cs = new SqliteConnectionStringBuilder { DataSource = path }.ToString();
        using var conn = new SqliteConnection(cs);
        conn.Open();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE Data (Id INTEGER PRIMARY KEY AUTOINCREMENT, Col0 TEXT);";
            cmd.ExecuteNonQuery();
        }
        using var tx = conn.BeginTransaction();
        using (var insert = conn.CreateCommand())
        {
            insert.Transaction = tx;
            insert.CommandText = "INSERT INTO Data (Col0) VALUES (@p);";
            var p = insert.CreateParameter();
            p.ParameterName = "@p";
            insert.Parameters.Add(p);
            for (int i = 0; i < rowCount; i++)
            {
                p.Value = $"row{i}";
                insert.ExecuteNonQuery();
            }
            tx.Commit();
        }
        return path;
    }

    public static DatabaseConfiguration CreateSchemaDatabase(int tables, int rowsPerTable, int columnsPerTable = 6)
    {
        var filePath = Path.Combine(CreateTempDirectory("schema"), $"schema_{tables}_{rowsPerTable}.db");
        var cs = new SqliteConnectionStringBuilder { DataSource = filePath }.ToString();
        using var conn = new SqliteConnection(cs);
        conn.Open();
        for (int t = 0; t < tables; t++)
        {
            var tableName = $"Table_{t}";
            using (var create = conn.CreateCommand())
            {
                var cols = new StringBuilder();
                cols.Append("Id INTEGER PRIMARY KEY AUTOINCREMENT");
                for (int c = 0; c < columnsPerTable - 1; c++)
                {
                    cols.Append($", Col{c} TEXT");
                }
                create.CommandText = $"CREATE TABLE {tableName} ({cols});";
                create.ExecuteNonQuery();
            }
            using var tx = conn.BeginTransaction();
            using var insert = conn.CreateCommand();
            insert.Transaction = tx;
            var colNames = Enumerable.Range(0, columnsPerTable - 1).Select(i => $"Col{i}").ToArray();
            var pNames = colNames.Select((_, i) => $"@p{i}").ToArray();
            insert.CommandText = $"INSERT INTO {tableName} ({string.Join(",", colNames)}) VALUES ({string.Join(",", pNames)});";
            for (int c = 0; c < pNames.Length; c++)
            {
                insert.Parameters.Add(new SqliteParameter(pNames[c], System.Data.DbType.String));
            }
            for (int r = 0; r < rowsPerTable; r++)
            {
                for (int c = 0; c < pNames.Length; c++)
                {
                    insert.Parameters[c].Value = $"t{t}_r{r}_c{c}";
                }
                insert.ExecuteNonQuery();
            }
            tx.Commit();
        }
        return DatabaseConfiguration.FromSqliteFile(filePath);
    }
}
