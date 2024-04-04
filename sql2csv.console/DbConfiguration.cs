namespace Sql2Csv;

/// <summary>
/// Represents the configuration for the database.
/// </summary>
public class DbConfiguration
{
    /// <summary>
    /// Gets or sets the name of the site.
    /// </summary>
    public string SiteName { get; set; }

    /// <summary>
    /// Gets or sets the connection string.
    /// </summary>
    public string ConnectionString { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbConfiguration"/> class with the specified database name.
    /// </summary>
    /// <param name="db">The name of the database.</param>
    public DbConfiguration(string db)
    {
        SiteName = db;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbConfiguration"/> class.
    /// </summary>
    public DbConfiguration()
    {
    }
}



