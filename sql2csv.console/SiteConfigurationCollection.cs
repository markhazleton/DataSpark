using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Sql2Csv;

public class SiteConfigurationCollection : List<DbConfiguration>
{
    private string _DataFolderPath = string.Empty;
    public string DataFolderPath
    {
        get { return _DataFolderPath; }
        set { _DataFolderPath = $"{Path.GetDirectoryName(value)}\\SiteConfiguration.xml"; }
    }

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



