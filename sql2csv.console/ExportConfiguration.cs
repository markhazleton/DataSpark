using System;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

namespace Sql2Csv;

/// <summary>
/// Represents the export configuration for SQL to CSV conversion.
/// </summary>
public class ExportConfiguration
{
    /// <summary>
    /// Gets or sets the path to the SQL script file.
    /// </summary>
    public string ScriptPath { get; set; }

    /// <summary>
    /// Gets or sets the path to the output CSV data file.
    /// </summary>
    public string DataPath { get; set; }

    /// <summary>
    /// Gets or sets the path to the configuration file.
    /// </summary>
    public string ConfigPath { get; set; }

    /// <summary>
    /// Gets or sets the path to the database configuration list file.
    /// </summary>
    public string DatabaseConfigurationListPath { get; set; }

    /// <summary>
    /// Gets or sets the name of the target database.
    /// </summary>
    public string TargetDatabaseName { get; set; }

    /// <summary>
    /// Validates the paths and ensures that the required directories exist.
    /// </summary>
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

    /// <summary>
    /// Saves the export configuration as an XML file.
    /// </summary>
    /// <returns>The path of the saved XML file.</returns>
    public string SaveXml()
    {
        var sReturn = string.Empty;
        var myXml = new XmlDocument();

        if (this != null)
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

    /// <summary>
    /// Retrieves the export configuration from an XML file.
    /// </summary>
    /// <returns>The export configuration object.</returns>
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

    /// <summary>
    /// Ensures that the specified directory path exists.
    /// </summary>
    /// <param name="path">The directory path to ensure.</param>
    public static void EnsurePathExists(string path)
    {
        try
        {
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
