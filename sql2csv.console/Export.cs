using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Sql2Csv;

/// <summary>
/// Provides methods for exporting SQL data to CSV format.
/// </summary>
public static class Export
{
    private const string STR_Quote = "\"";
    private const string STR_VBTab = "\t";

    /// <summary>
    /// Executes a SQL query and returns the result as a DataTable.
    /// </summary>
    /// <param name="sSQL">The SQL query.</param>
    /// <param name="SQLDBConnString">The database connection string.</param>
    /// <returns>The result of the SQL query as a DataTable.</returns>
    private static DataTable GetDataTable(string sSQL, string SQLDBConnString)
    {
        using var dataTable = new DataTable();
        using (var RecConn = new SqliteConnection { ConnectionString = SQLDBConnString })
        {
            try
            {
                RecConn.Open();
                using var myCommand = new SqliteCommand(sSQL, RecConn);
                myCommand.CommandTimeout = 600;
                var myDR = myCommand.ExecuteReader();
                dataTable.Load(myDR);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e} Exception caught.");
            }
        }
        return dataTable;
    }

    /// <summary>
    /// Checks if a value is numeric.
    /// </summary>
    /// <param name="value">The value to be checked.</param>
    /// <returns>True if the value is numeric, otherwise false.</returns>
    private static bool IsNumeric(string value)
    {
        return int.TryParse(value, out _);
    }

    /// <summary>
    /// Processes a SQL file and exports the data to CSV format.
    /// </summary>
    /// <param name="myExportConfig">The export configuration.</param>
    /// <param name="Response">The response StringBuilder.</param>
    /// <param name="Sql">The SQL statement.</param>
    /// <param name="Dt">The DataTable to store the query result.</param>
    /// <param name="Message">The message to be appended to the response.</param>
    /// <param name="mySite">The database configuration.</param>
    /// <param name="myFile">The path of the SQL file.</param>
    private static void ProcessSqlFile(ExportConfiguration myExportConfig, StringBuilder Response, ref string Sql, ref DataTable Dt, ref string Message, DbConfiguration mySite, string myFile)
    {
        try
        {
            var myStopWatch = new Stopwatch();
            myStopWatch.Start();
            Sql = File.ReadAllText(myFile);
            Dt = GetDataTable(Sql, mySite.ConnectionString);

            var SiteDataFolder = myExportConfig.DataPath;
            if (!Directory.Exists(SiteDataFolder))
            {
                Directory.CreateDirectory(SiteDataFolder);
            }
            var SiteFileName = $"{SiteDataFolder}\\{mySite.DatabaseName}-{myFile.Replace(myExportConfig.ScriptPath, string.Empty).Replace(".sql", string.Empty)}.txt";

            SiteFileName = $"{SiteDataFolder}\\{myFile.Replace(myExportConfig.ScriptPath, string.Empty).Replace(".sql", string.Empty)}.csv";
            using (var writer = new StreamWriter(SiteFileName, false))
            {
                WriteDataTableCsv(Dt, writer, true);
            }

            myStopWatch.Stop();
            Message = $"{mySite.DatabaseName} - {myFile.Replace(myExportConfig.ScriptPath, string.Empty)} time:{myStopWatch.ElapsedMilliseconds}";
            Response.AppendLine(Message);
            Console.WriteLine(Message);
        }
        catch (Exception e)
        {
            Console.WriteLine($"{e} Exception caught.");
        }
    }

    /// <summary>
    /// Quotes a value for CSV format.
    /// </summary>
    /// <param name="value">The value to be quoted.</param>
    /// <returns>The quoted value.</returns>
    private static string QuoteCSVValue(object value)
    {
        var valType = value.GetType();
        switch (valType.Name.ToUpper())
        {
            case "BYTE":
            case "DECIMAL":
            case "DOUBLE":
            case "INT16":
            case "INT32":
            case "INT64":
                return value.ToString();
            case "DATETIME":
                if (((DateTime)value).TimeOfDay.TotalSeconds == 0)
                {
                    return String.Concat("\"", ((DateTime)value).ToShortDateString().Replace("\"", "\"\""), "\"");
                }
                else
                {
                    return String.Concat("\"", ((DateTime)value).ToString().Replace("\"", "\"\""), "\"");
                }
            case "STRING":
                return String.Concat("\"", value.ToString().Replace("\"", "\"\""), "\"");
            case "DBNULL":
                return String.Concat("\"", value.ToString().Replace("\"", "\"\""), "\"");
            default:
                Console.WriteLine($"New Type:{valType.Name.ToUpper()}");
                return String.Concat("\"", value.ToString().Replace("\"", "\"\""), "\"");
        }
    }

    /// <summary>
    /// Quotes a value for CSV format.
    /// </summary>
    /// <param name="value">The value to be quoted.</param>
    /// <returns>The quoted value.</returns>
    private static string QuoteValue(string value)
    {
        return value;
    }

    /// <summary>
    /// Quotes a string value for CSV format.
    /// </summary>
    /// <param name="value">The string value to be quoted.</param>
    /// <returns>The quoted string value.</returns>
    public static string GetQuotedString(string value)
    {
        if (IsNumeric(value))
        {
            return value;
        }
        else
        {
            return $"{STR_Quote}{value}{STR_Quote}";
        }
    }

    /// <summary>
    /// Processes the extraction SQL for all sites and exports the data to CSV format.
    /// </summary>
    /// <param name="mySiteList">The collection of site configurations.</param>
    /// <param name="myExportConfig">The export configuration.</param>
    /// <returns>The exported data in CSV format.</returns>
    public static string ProcessExtractSql(DbConfigurationCollection mySiteList, ExportConfiguration myExportConfig)
    {
        var Response = new StringBuilder();
        var Sql = string.Empty;

        var Dt = new DataTable();
        var Message = string.Empty;

        var ext = new List<string> { ".sql" };
        IEnumerable<string> mySQLFiles;
        IEnumerable<string> allSQLFiles;
        try
        {
            allSQLFiles = Directory.GetFiles(myExportConfig.ScriptPath).ToList();
            if (!allSQLFiles.Any())
            {
                Console.WriteLine("No SQL files found in the script path.");
                // Create a default.sql file with the sql statement to be executed
                var defaultSql = "SELECT id,name FROM Test";
                var defaultSqlPath = Path.Combine(myExportConfig.ScriptPath, "default.sql");
                File.WriteAllText(defaultSqlPath, defaultSql);
                Console.WriteLine($"Created default.sql file with the following content: {defaultSql}");
                mySQLFiles = [defaultSqlPath];
            }

            mySQLFiles = Directory.GetFiles(myExportConfig.ScriptPath, "*.*", SearchOption.AllDirectories).Where(s => ext.Any(e => s.EndsWith(e))).ToList();
            foreach (DbConfiguration mySite in mySiteList)
            {
                foreach (string myFile in mySQLFiles)
                {
                    ProcessSqlFile(myExportConfig, Response, ref Sql, ref Dt, ref Message, mySite, myFile);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"{e} Exception caught.");
        }
        return Response.ToString();
    }

    /// <summary>
    /// Writes the data from a DataTable to a TextWriter in CSV format.
    /// </summary>
    /// <param name="sourceTable">The source DataTable.</param>
    /// <param name="writer">The TextWriter to write the CSV data to.</param>
    /// <param name="includeHeaders">Specifies whether to include column headers in the CSV output.</param>
    public static void WriteDataTable(DataTable sourceTable, TextWriter writer, bool includeHeaders)
    {
        var myRow = string.Empty;
        if ((includeHeaders))
        {
            var headerValues = sourceTable.Columns.OfType<DataColumn>().Select(column => QuoteValue(column.ColumnName));
            writer.WriteLine(string.Join(STR_VBTab, headerValues));
        }
        foreach (DataRow row in sourceTable.Rows)
        {
            myRow = string.Join(STR_VBTab, row.ItemArray.Select(obj => QuoteValue(obj.ToString())));
            writer.WriteLine(myRow);
        }
        writer.Flush();
    }

    /// <summary>
    /// Writes the data from a DataTable to a TextWriter in CSV format.
    /// </summary>
    /// <param name="sourceTable">The source DataTable.</param>
    /// <param name="writer">The TextWriter to write the CSV data to.</param>
    /// <param name="includeHeaders">Specifies whether to include column headers in the CSV output.</param>
    public static void WriteDataTableCsv(DataTable sourceTable, TextWriter writer, bool includeHeaders)
    {
        if (includeHeaders)
        {
            var headerValues = sourceTable.Columns
                .OfType<DataColumn>()
                .Select(column => QuoteCSVValue(column.ColumnName));

            writer.WriteLine(String.Join(",", headerValues));
        }

        IEnumerable<String> items = null;

        foreach (DataRow row in sourceTable.Rows)
        {
            items = row.ItemArray.Select(o => QuoteCSVValue(o));
            writer.WriteLine(String.Join(",", items));
        }

        writer.Flush();
    }
}
