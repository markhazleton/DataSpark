namespace Sql2Csv.Core.Configuration;

/// <summary>
/// Configuration options for the SQL to CSV application.
/// </summary>
public sealed class Sql2CsvOptions
{
    public const string SectionName = "Sql2Csv";

    /// <summary>
    /// Gets or sets the root path for the application.
    /// </summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path configuration.
    /// </summary>
    public PathOptions Paths { get; set; } = new();

    /// <summary>
    /// Gets or sets the database configuration.
    /// </summary>
    public DatabaseOptions Database { get; set; } = new();

    /// <summary>
    /// Gets or sets the export configuration.
    /// </summary>
    public ExportOptions Export { get; set; } = new();
}

/// <summary>
/// Path configuration options.
/// </summary>
public sealed class PathOptions
{
    /// <summary>
    /// Gets or sets the configuration path.
    /// </summary>
    public string Config { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data path.
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the scripts path.
    /// </summary>
    public string Scripts { get; set; } = string.Empty;
}

/// <summary>
/// Database configuration options.
/// </summary>
public sealed class DatabaseOptions
{
    /// <summary>
    /// Gets or sets the default database name.
    /// </summary>
    public string DefaultName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the command timeout in seconds.
    /// </summary>
    public int Timeout { get; set; } = 600;
}

/// <summary>
/// Export configuration options.
/// </summary>
public sealed class ExportOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to include headers in the export.
    /// </summary>
    public bool IncludeHeaders { get; set; } = true;

    /// <summary>
    /// Gets or sets the CSV delimiter.
    /// </summary>
    public string Delimiter { get; set; } = ",";

    /// <summary>
    /// Gets or sets the encoding for the export files.
    /// </summary>
    public string Encoding { get; set; } = "UTF-8";
}
