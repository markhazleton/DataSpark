using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Xml;



namespace Sql2Csv
{
public class DbConfiguration
{
    public string SiteName { get; set; }
    public string ConnectionString { get; set; }
    public DbConfiguration(string db)
    {
        SiteName = db;
    }
    public DbConfiguration()
    {
    }
}
}



