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



