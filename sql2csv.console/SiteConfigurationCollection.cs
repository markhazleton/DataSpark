using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Sql2Csv;

/// <summary>
/// Represents a collection of site configurations.
/// </summary>
public class SiteConfigurationCollection : List<DbConfiguration>
{
    private string _DataFolderPath = string.Empty;

    /// <summary>
    /// Gets or sets the data folder path.
    /// </summary>
    public string DataFolderPath
    {
        get { return _DataFolderPath; }
        set { _DataFolderPath = $"{Path.GetDirectoryName(value)}\\SiteConfiguration.xml"; }
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
                var oXS = new XmlSerializer(typeof(SiteConfigurationCollection));
                using (var writer = new StringWriter())
                {
                    oXS.Serialize(writer, this);
                    myXml.LoadXml(writer.ToString());
                }
                myXml.Save(_DataFolderPath);

            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("Error on Save Site Configuration List: {0}", ex));
                ReturnStrimg = ex.ToString();
            }

        }
        return ReturnStrimg;
    }

    /// <summary>
    /// Loads the collection from an XML file.
    /// </summary>
    /// <param name="defaultConnection">The default connection string.</param>
    /// <param name="defaultName">The default site name.</param>
    /// <returns>The loaded SiteConfigurationCollection.</returns>
    public SiteConfigurationCollection GetFromXml(string defaultConnection, string defaultName)
    {
        Clear();
        var x = new XmlSerializer(typeof(SiteConfigurationCollection));
        Console.WriteLine(string.Format("Site Configuration Collection  **** Path: {0}", _DataFolderPath));

        try
        {
            if (File.Exists(_DataFolderPath))
            {
                using var objStreamReader = new StreamReader(_DataFolderPath);
                var myY = (SiteConfigurationCollection)x.Deserialize(objStreamReader);
                AddRange(myY);
            }
            else
            {
                var myList = new SiteConfigurationCollection
                    {
                        new DbConfiguration() { ConnectionString = defaultConnection, SiteName = defaultName}
                    };
                myList._DataFolderPath = _DataFolderPath.Replace(".xml", "_new.xml");
                myList.SaveXml();
                AddRange(myList);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(string.Format("Site Configuration Collection  **** ERROR: {0}", ex));
        }
        return this;
    }
}



