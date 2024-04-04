using System;
using System.IO;
using System.Reflection;

namespace Sql2Csv;

class Program
{
    static void Main(string[] args)
    {
        var myExportConfig = new ExportConfiguration();
        var path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        myExportConfig.ConfigPath = $"{path}\\config\\";
        myExportConfig.GetFromXml();
        myExportConfig.ValidatePaths();
        DisplayExportConfig(myExportConfig);

        var defaultDb = SqliteDatabaseCreator.CreateDatabaseAndTable();
        Console.WriteLine($"Default database connection string: {defaultDb}");

        var myConfigList = new SiteConfigurationCollection()
        {
            DataFolderPath = myExportConfig.DatabaseConfigurationListPath
        };
        myConfigList.GetFromXml(defaultDb, "default");
        Export.ProcessExtractSql(myConfigList, myExportConfig);
        Console.ReadKey();

        static void DisplayExportConfig(ExportConfiguration myExportConfig)
        {
            Console.WriteLine("          **** ");
            Console.WriteLine("          **** ");
            Console.WriteLine("          **** ");
            Console.WriteLine($"          **** Configuration Path: {myExportConfig.ConfigPath}");
            Console.WriteLine($"          **** Data Path: {myExportConfig.DataPath}");
            Console.WriteLine($"          **** Script Path: {myExportConfig.ScriptPath}");
            Console.WriteLine(
                string.Format("          **** Configuration List: {0}", myExportConfig.DatabaseConfigurationListPath));
            Console.WriteLine("          **** ");
            Console.WriteLine("          **** ");
            Console.WriteLine("          **** ");
        }
    }
}
