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

        Console.WriteLine("          **** ");
        Console.WriteLine("          **** ");
        Console.WriteLine("          **** ");
        Console.WriteLine(string.Format("          **** Configuration Path: {0}", myExportConfig.ConfigPath));
        Console.WriteLine(string.Format("          **** Data Path: {0}", myExportConfig.DataPath));
        Console.WriteLine(string.Format("          **** Script Path: {0}", myExportConfig.ScriptPath));
        Console.WriteLine(string.Format("          **** Configuration List: {0}", myExportConfig.DatabaseConfigurationListPath));
        Console.WriteLine("          **** ");
        Console.WriteLine("          **** ");
        Console.WriteLine("          **** ");

        EnsurePathExists(myExportConfig.ConfigPath);
        EnsurePathExists(myExportConfig.DataPath);
        EnsurePathExists(myExportConfig.ScriptPath);
        EnsurePathExists(myExportConfig.DatabaseConfigurationListPath);

        var myConfigList = new SiteConfigurationCollection() { DataFolderPath = myExportConfig.DatabaseConfigurationListPath };
        myConfigList.GetFromXml();
        Export.ProcessExtractSql(myConfigList, myExportConfig);

        Console.ReadKey();



    }
    public static void EnsurePathExists(string path)
    {
        // ... Set to folder path we must ensure exists.
        try
        {
            // ... If the directory doesn't exist, create it.
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        catch (Exception)
        {
            // Fail silently.
        }
    }
}
