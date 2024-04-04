using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Sql2Csv;

public static class Export
{
    private const string STR_Quote = "\"";
    private const string STR_VBTab = "\t";
    public static string ProcessExtractSql(SiteConfigurationCollection mySiteList, ExportConfiguration myExportConfig)
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
            if(!allSQLFiles.Any())
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
            Console.WriteLine("{0} Exception caught.", e);
        }
        return Response.ToString();

        static void ProcessSqlFile(ExportConfiguration myExportConfig, StringBuilder Response, ref string Sql, ref DataTable Dt, ref string Message, DbConfiguration mySite, string myFile)
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
                var SiteFileName = string.Format("{0}\\{1}-{2}.txt", SiteDataFolder, mySite.SiteName, myFile.Replace(myExportConfig.ScriptPath, string.Empty).Replace(".sql", string.Empty));

                SiteFileName = string.Format("{0}\\{1}.csv", SiteDataFolder, myFile.Replace(myExportConfig.ScriptPath, string.Empty).Replace(".sql", string.Empty));
                using (var writer = new StreamWriter(SiteFileName, false))
                {
                    WriteDataTableCsv(Dt, writer, true);
                }

                myStopWatch.Stop();
                Message = string.Format("{1} - {0} time:{2}", myFile.Replace(myExportConfig.ScriptPath, string.Empty), mySite.SiteName, myStopWatch.ElapsedMilliseconds);
                Response.AppendLine(Message);
                Console.WriteLine(Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e);
            }
        }
    }
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
                Console.WriteLine("{0} Exception caught.", e);
            }
        }
        return dataTable;
    }
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
            row.ItemArray.Select(field => field.ToString());
            myRow = string.Join(STR_VBTab, row.ItemArray.Select(obj => QuoteValue(obj.ToString())));
            writer.WriteLine(myRow);
        }
        writer.Flush();
    }
    private static string QuoteValue(string value)
    {
        if (IsNumeric(value))
        {
            return value;
        }
        else
        {
            return value;
        }
    }
    public static string GetQuotedString(string value)
    {
        if (IsNumeric(value))
        {
            return value;
        }
        else
        {
            return string.Format("{0}{1}{0}", STR_Quote, value);
        }
    }
    private static bool IsNumeric(string value)
    {
        int n;
        return int.TryParse(value, out n);
    }
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
                Console.WriteLine(string.Format("New Type:{0}", valType.Name.ToUpper()));
                return String.Concat("\"", value.ToString().Replace("\"", "\"\""), "\"");
        }
    }
}
