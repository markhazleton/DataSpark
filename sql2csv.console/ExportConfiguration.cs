using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Sql2Csv;

/// <summary>
/// Represents the export configuration for SQL to CSV conversion.
/// </summary>
public class ExportConfiguration
{
    private readonly string _rootPath;

    public ExportConfiguration()
    {
        _rootPath = "C:\\Temp\\SQL2CSV";
    }
    public ExportConfiguration(string path)
    {
        _rootPath = path;
    }

    /// <summary>
    /// Validates the paths and ensures that the required directories exist.
    /// </summary>
    private void ValidatePaths()
    {
        ConfigPath ??= $"{_rootPath}\\config\\";
        ScriptPath ??= $"{_rootPath}\\script\\";
        DataPath ??= $"{_rootPath}\\data\\";
        DatabaseConfigurationListPath ??= $"{_rootPath}\\config\\";

        EnsurePathExists(ConfigPath);
        EnsurePathExists(DataPath);
        EnsurePathExists(ScriptPath);
        EnsurePathExists(DatabaseConfigurationListPath);
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

    /// <summary>
    /// Retrieves the export configuration from an XML file.
    /// </summary>
    /// <returns>The export configuration object.</returns>
    public ExportConfiguration GetFromXml()
    {
        var x = new XmlSerializer(typeof(ExportConfiguration));
        try
        {
            ConfigPath = $"{_rootPath}\\config\\";
            var path = $"{ConfigPath}ExportConfiguration.xml";
            // Check if the file exists
            if (!File.Exists(path))
            {
                SaveXml();
            }
            using var objStreamReader = new StreamReader($"{ConfigPath}\\ExportConfiguration.xml");
            var myY = (ExportConfiguration)x.Deserialize(objStreamReader);
            ConfigPath = myY.ConfigPath;
            DataPath = myY.DataPath;
            ScriptPath = myY.ScriptPath;
            DatabaseConfigurationListPath = myY.DatabaseConfigurationListPath;
            TargetDatabaseName = myY.TargetDatabaseName;
        }
        catch (Exception e)
        {
            Console.WriteLine($"{e} Exception caught.");
        }
        SaveXml();
        DisplayExportConfig();
        return this;
    }

    public void DisplayExportConfig()
    {
        Console.WriteLine("          **** ");
        Console.WriteLine("          **** ");
        Console.WriteLine("          **** ");
        Console.WriteLine($"          **** Configuration Path: {ConfigPath}");
        Console.WriteLine($"          **** Data Path: {DataPath}");
        Console.WriteLine($"          **** Script Path: {ScriptPath}");
        Console.WriteLine($"          **** Configuration List: {DatabaseConfigurationListPath}");
        Console.WriteLine("          **** ");
        Console.WriteLine("          **** ");
        Console.WriteLine("          **** ");
    }



    /// <summary>
    /// Saves the export configuration as an XML file.
    /// </summary>
    /// <returns>The path of the saved XML file.</returns>
    public string SaveXml()
    {
        ValidatePaths();
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
            myXml.Save($"{ConfigPath}\\ExportConfiguration.xml");
        }
        return sReturn;
    }

    /// <summary>
    /// Gets or sets the path to the configuration file.
    /// </summary>
    public string ConfigPath { get; set; }

    /// <summary>
    /// Gets or sets the path to the database configuration list file.
    /// </summary>
    public string DatabaseConfigurationListPath { get; set; }

    /// <summary>
    /// Gets or sets the path to the output CSV data file.
    /// </summary>
    public string DataPath { get; set; }
    /// <summary>
    /// Gets or sets the path to the SQL script file.
    /// </summary>
    public string ScriptPath { get; set; }

    /// <summary>
    /// Gets or sets the name of the target database.
    /// </summary>
    public string TargetDatabaseName { get; set; }
}
