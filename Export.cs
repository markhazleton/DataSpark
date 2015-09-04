using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Linq;


public class Export
{
    private const string STR_Quote = "\"";
    private const string STR_VBTab = "\t";

    public string ProcessExtractSQL(ref SiteConfigurationList mySiteList, ref ExportConfiguration myExportConfig)
    {
        var myResponse = new StringBuilder();
        var mySQL = string.Empty;

        var myDT = new DataTable();
        var myMessage = string.Empty;

        var ext = new List<string> { ".sql" };
        IEnumerable<string> mySQLFiles;
        IEnumerable<string> allSQLFiles;
        try
        {
            allSQLFiles = Directory.GetFiles(myExportConfig.ScriptPath).ToList();

            mySQLFiles = Directory.GetFiles(myExportConfig.ScriptPath, "*.*", SearchOption.AllDirectories).Where(s => ext.Any(e => s.EndsWith(e))).ToList();
            foreach (DBConfiguration mySite in mySiteList)
            {
                foreach (string myFile in mySQLFiles)
                {
                    try
                    {
                        var myStopWatch = new Stopwatch();
                        myStopWatch.Start();
                        mySQL = File.ReadAllText(myFile);
                        myDT = GetDataTable(mySQL, mySite.ConnectionString);

                        var SiteDataFolder = myExportConfig.DataPath;
                        if (!Directory.Exists(SiteDataFolder))
                        {
                            Directory.CreateDirectory(SiteDataFolder);
                        }
                        var SiteFileName = string.Format("{0}\\{1}-{2}.txt", SiteDataFolder, mySite.SiteName, myFile.Replace(myExportConfig.ScriptPath, string.Empty).Replace(".sql", string.Empty));

                        SiteFileName = string.Format("{0}\\{1}.csv", SiteDataFolder, myFile.Replace(myExportConfig.ScriptPath, string.Empty).Replace(".sql", string.Empty));
                        using (var writer = new StreamWriter(SiteFileName, false))
                        {
                            WriteDataTableCSV(myDT, writer, true);
                        }

                        myStopWatch.Stop();
                        myMessage = string.Format("{1} - {0} time:{2}", myFile.Replace(myExportConfig.ScriptPath, string.Empty), mySite.SiteName, myStopWatch.ElapsedMilliseconds);
                        myResponse.AppendLine(myMessage);
                        Console.WriteLine(myMessage);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("{0} Exception caught.", e);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("{0} Exception caught.", e);
        }
        return myResponse.ToString();
    }

    private DataTable GetDataTable(string sSQL, string wpm_SQLDBConnString)
    {
        using (var dataTable = new DataTable())
        {
            using (var RecConn = new System.Data.SqlClient.SqlConnection { ConnectionString = wpm_SQLDBConnString })
            {
                try
                {
                    RecConn.Open();
                    using (var myCommand = new System.Data.SqlClient.SqlCommand(sSQL, RecConn))
                    {
                        myCommand.CommandTimeout = 60000;
                        var myDR = myCommand.ExecuteReader();
                        dataTable.Load(myDR);
                    }
                    RecConn.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine("{0} Exception caught.", e);
                }
            }
            return dataTable;
        }
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

    public static string GetQuotedString(string Value)
    {
        if (IsNumeric(Value))
        {
            return Value;
        }
        else
        {
            return string.Format("{0}{1}{0}", STR_Quote, Value);
        }
    }

    private static bool IsNumeric(string Value)
    {
        int n;
        return int.TryParse(Value, out n);
    }


    public static void WriteDataTableCSV(DataTable sourceTable, TextWriter writer, bool includeHeaders)
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
                return value.ToString();
                break;
            case "DECIMAL":
                return value.ToString();
                break;
            case "INT16":
                return value.ToString();
                break;
            case "INT32":
                return value.ToString();
                break;
            case "DATETIME":
                if (((DateTime)value).TimeOfDay.TotalSeconds == 0)
                {
                    return String.Concat("\"", ((DateTime)value).ToShortDateString().Replace("\"", "\"\""), "\"");
                }
                else
                {
                    return String.Concat("\"", ((DateTime)value).ToString().Replace("\"", "\"\""), "\"");
                }
                break;
            case "STRING":
                return String.Concat("\"", value.ToString().Replace("\"", "\"\""), "\"");
                break;
            case "DBNULL":
                return String.Concat("\"", value.ToString().Replace("\"", "\"\""), "\"");
                break;
            default:
                Console.WriteLine(string.Format("New Type:{0}", valType.Name.ToUpper()));
                return String.Concat("\"", value.ToString().Replace("\"", "\"\""), "\"");
                break;
        }

        return String.Concat("\"",
        value.ToString().Replace("\"", "\"\""), "\"");
    }
}
