using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Csv
{
    class Program
    {
        static void Main(string[] args)
        {
            var myExportConfig = new ExportConfiguration();
            var path = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            myExportConfig.ConfigPath = System.IO.Path.GetDirectoryName(path).Replace("file:\\", string.Empty);
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

            var myConfigList = new SiteConfigurationCollection() { DataFolderPath = myExportConfig.DatabaseConfigurationListPath };
            myConfigList.GetFromXml();
            Export.ProcessExtractSql(myConfigList, myExportConfig);
        }
    }
}
