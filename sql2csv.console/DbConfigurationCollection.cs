using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace Sql2Csv;

/// <summary>
/// Represents a collection of site configurations.
/// </summary>
public class DbConfigurationCollection : List<DbConfiguration>
{
    private string _DataFolderPath;

    public DbConfigurationCollection()
    {
        _DataFolderPath = string.Empty;
    }
    public DbConfigurationCollection(string dataFolderPath)
    {
        _DataFolderPath = dataFolderPath;
        DiscoverDatabases();
    }
    public void DiscoverDatabases()
    {
        // Search for databases in the _DataFolderPath
        var dbFiles = Directory.GetFiles(_DataFolderPath, "*.db");
        // Add them to the collection
        foreach (var dbFile in dbFiles)
        {
            //  check to make sure it is not already in the collection
            if (this.Any(x => x.ConnectionString == $"Data Source={dbFile}")) continue;
            Add(new DbConfiguration(dbFile));
        }
        SaveXml();
    }

    /// <summary>
    /// Loads the collection from an XML file.
    /// </summary>
    /// <param name="defaultConnection">The default connection string.</param>
    /// <param name="defaultName">The default site name.</param>
    /// <returns>The loaded SiteConfigurationCollection.</returns>
    public DbConfigurationCollection GetFromXml(string defaultConnection, string defaultName)
    {
        Clear();
        var x = new XmlSerializer(typeof(DbConfigurationCollection));
        Console.WriteLine(string.Format("Site Configuration Collection  **** Path: {0}", GetFileName()));

        try
        {
            if (File.Exists(GetFileName()))
            {
                using var objStreamReader = new StreamReader(GetFileName());
                var myY = (DbConfigurationCollection)x.Deserialize(objStreamReader);
                AddRange(myY);
            }
            else
            {
                var myList = new DbConfigurationCollection
                    {
                        new DbConfiguration(defaultConnection, defaultName)
                    };
                AddRange(myList);
                myList.SaveXml();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(string.Format("Site Configuration Collection  **** ERROR: {0}", ex));
        }
        return this;
    }

    /// <summary>
    /// Saves the collection as an XML file.
    /// </summary>
    /// <returns>The error message if an exception occurs, otherwise an empty string.</returns>
    public string SaveXml()
    {
        var ReturnStrimg = string.Empty;
        var myXml = new XmlDocument();

        if ((this != null))
        {
            try
            {
                var oXS = new XmlSerializer(typeof(DbConfigurationCollection));
                using (var writer = new StringWriter())
                {
                    oXS.Serialize(writer, this);
                    myXml.LoadXml(writer.ToString());
                }
                myXml.Save(GetFileName());

            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("Error on Save Site Configuration List: {0}", ex));
                ReturnStrimg = ex.ToString();
            }

        }
        return ReturnStrimg;
    }

    private string GetFileName()
    {
        return $"{_DataFolderPath}\\DatabaseConfiguration.xml";
    }

    /// <summary>
    /// Gets or sets the data folder path.
    /// </summary>
    public string DataFolderPath
    {
        get { return _DataFolderPath; }
    }
}



