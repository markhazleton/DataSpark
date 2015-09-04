using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Xml;

namespace Sql2Csv
{

    public class SiteConfigurationCollection : List<DbConfiguration>
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

        public SiteConfigurationCollection GetFromXml()
        {
            Clear();
            var x = new XmlSerializer(typeof(SiteConfigurationCollection));
            Console.WriteLine(string.Format("Site Configuration Collection  **** Path: {0}", _DataFolderPath));

            try
            {
                using (var objStreamReader = new StreamReader(_DataFolderPath))
                {
                    var myY = (SiteConfigurationCollection)x.Deserialize(objStreamReader);
                    AddRange(myY);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Site Configuration Collection  **** ERROR: {0}", ex));

                var myList = new SiteConfigurationCollection();
                myList.Add(new DbConfiguration() { ConnectionString = "Data Source=HAPXSSQLL01\\FAC_LIVE;Initial Catalog=FreeFlight_App;Integrated Security=True;", SiteName = "FreeFlight" });
                myList._DataFolderPath = _DataFolderPath.Replace(".xml", "_new.xml");
                myList.SaveXml();
            }
            return this;
        }
    }
}