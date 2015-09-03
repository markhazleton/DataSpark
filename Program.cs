using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sql2csv
{
    class Program
    {
        static void Main(string[] args)
        {
            var myExportConfig = new ExportConfiguration();
            var path = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            myExportConfig.ConfigPath = System.IO.Path.GetDirectoryName(path).Replace("file:\\", string.Empty);
            myExportConfig.GetFromXML();
            Console.WriteLine(string.Format("GotExport **** ConfigPath: {0}", myExportConfig.ConfigPath));
            Console.WriteLine(string.Format("          **** DataPath: {0}", myExportConfig.DataPath));
            Console.WriteLine(string.Format("          **** ScriptPath: {0}", myExportConfig.ScriptPath));
            Console.WriteLine(string.Format("          **** ConfigList: {0}", myExportConfig.DatabaseConfigurationListPath));
            var myExport = new Export();
            var myConfigList = new SiteConfigurationList();
            myConfigList.DataFolderPath = myExportConfig.DatabaseConfigurationListPath;
            myConfigList.GetFromXML();
            myExport.ProcessExtractSQL(ref myConfigList, ref myExportConfig);
        }
    }
}
