namespace Sql2Csv;

/// <summary>
/// Represents the configuration for the database.
/// </summary>
public class DbConfiguration
{
    /// <summary>
    /// Gets or sets the name of the site.
    /// </summary>
    public string DatabaseName { get; set; }

    /// <summary>
    /// Gets or sets the connection string.
    /// </summary>
    public string ConnectionString { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbConfiguration"/> class with the specified database name.
    /// </summary>
    /// <param name="db">The name of the database.</param>
    public DbConfiguration(string db, string conectionString)
    {
        DatabaseName = db;
        ConnectionString = conectionString;
    }
    public DbConfiguration(string dbFile)
    {
        DatabaseName = dbFile.Replace(".db", string.Empty);
        ConnectionString = $"Data Source={dbFile}"; ;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbConfiguration"/> class.
    /// </summary>
    public DbConfiguration()
    {
    }
}



