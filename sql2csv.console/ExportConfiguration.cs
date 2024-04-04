using System;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

namespace Sql2Csv;

public class ExportConfiguration
{
    public string ScriptPath { get; set; }
    public string DataPath { get; set; }
    public string ConfigPath { get; set; }
    public string DatabaseConfigurationListPath { get; set; }
    public string TargetDatabaseName { get; set; }
    public void ValidatePaths()
    {
        var path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        ConfigPath ??= $"{path}\\config\\";
        ScriptPath ??= $"{path}\\script\\";
        DataPath ??= $"{path}\\data\\";
        DatabaseConfigurationListPath ??= $"{path}\\config\\";


        EnsurePathExists(ConfigPath);
        EnsurePathExists(DataPath);
        EnsurePathExists(ScriptPath);
        EnsurePathExists(DatabaseConfigurationListPath);
    }

    public string SaveXml()
    {
        var sReturn = string.Empty;
        var myXml = new XmlDocument();

        if ((this != null))
        {
            var oXS = new XmlSerializer(typeof(ExportConfiguration));
            using (var writer = new StringWriter())
            {
                oXS.Serialize(writer, this);
                myXml.LoadXml(writer.ToString());
            }
            myXml.Save(string.Format("{0}\\ExportConfiguration.xml", ConfigPath));
        }
        return sReturn;
    }
    public ExportConfiguration GetFromXml()
    {
        var x = new XmlSerializer(typeof(ExportConfiguration));
        try
        {
            using var objStreamReader = new StreamReader(string.Format("{0}\\ExportConfiguration.xml", ConfigPath));
            var myY = (ExportConfiguration)x.Deserialize(objStreamReader);
            ConfigPath = myY.ConfigPath;
            DataPath = myY.DataPath;
            ScriptPath = myY.ScriptPath;
            DatabaseConfigurationListPath = myY.DatabaseConfigurationListPath;
            TargetDatabaseName = myY.TargetDatabaseName;
        }
        catch (Exception e)
        {
            ExportConfiguration myExportConfig = new ExportConfiguration();
            var path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            myExportConfig.ConfigPath = $"{path}\\config\\";
            myExportConfig.ScriptPath = $"{path}\\script\\";
            myExportConfig.DataPath = $"{path}\\data\\";
            myExportConfig.DatabaseConfigurationListPath = $"{path}\\config\\";
            EnsurePathExists(myExportConfig.ConfigPath);
            EnsurePathExists(myExportConfig.DataPath);
            EnsurePathExists(myExportConfig.ScriptPath);
            EnsurePathExists(myExportConfig.DatabaseConfigurationListPath);

            myExportConfig.SaveXml();
            Console.WriteLine("{0} Exception caught.", e);
        }
        return this;
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
