public class DBConfiguration
{
    public string SiteName { get; set; }
    public string ConnectionString { get; set; }


    public DBConfiguration(string DBName)
    {
        SiteName = DBName;
    }

    public DBConfiguration()
    {
    }
}
