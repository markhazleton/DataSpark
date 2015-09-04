using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Xml;

public class SiteConfigurationList : List<DBConfiguration>
{
    private string _DataFolderPath = string.Empty;
    public string DataFolderPath
    {
        get
        {
            return _DataFolderPath;
        }
        set
        {
            _DataFolderPath = value;
        }
    }

    public string SaveXML()
    {
        var sReturn = string.Empty;
        var myXML = new XmlDocument();

        if ((this != null))
        {
            try
            {
                var oXS = new XmlSerializer(typeof(SiteConfigurationList));
                using (var writer = new StringWriter())
                {
                    oXS.Serialize(writer, this);
                    myXML.LoadXml(writer.ToString());
                }
                myXML.Save(_DataFolderPath);

            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("Error on Save Site Configuration List: {0}", ex.ToString()));
                sReturn = ex.ToString();
            }

        }
        return sReturn;
    }

    public SiteConfigurationList GetFromXML()
    {
        Clear();
        var x = new XmlSerializer(typeof(SiteConfigurationList));
        Console.WriteLine(string.Format("SiteConfigurationList  **** Path: {0}", _DataFolderPath));

        try
        {
            using (var objStreamReader = new StreamReader(_DataFolderPath))
            {
                var myY = (SiteConfigurationList)x.Deserialize(objStreamReader);
                AddRange(myY);
                objStreamReader.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(string.Format("SiteConfigurationList  **** ERROR: {0}", ex));

            var myList = new SiteConfigurationList();
            myList.Add(new DBConfiguration() { ConnectionString = "Data Source=HAPXSSQLL01\\FAC_LIVE;Initial Catalog=FreeFlight_App;Integrated Security=True;", SiteName = "FreeFlight" });
            myList._DataFolderPath = _DataFolderPath.Replace(".xml", "_new.xml");
            myList.SaveXML();
        }
        return this;
    }
}
