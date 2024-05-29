
using Sql2Csv;
using System;
using System.IO;

var _rootPath = "C:\\Temp\\SQL2CSV";
var myExportConfig = new ExportConfiguration(_rootPath);
myExportConfig.GetFromXml();

var defaultConnectionString = (new SqliteDatabaseCreator(_rootPath)).ConnectionString;
Console.WriteLine($"Default database connection string: {defaultConnectionString}");

var databaseCollection = new DbConfigurationCollection(myExportConfig.DataPath);
databaseCollection.GetFromXml(defaultConnectionString, "default");
Console.WriteLine($"Database count: {databaseCollection.Count}");

foreach (var db in databaseCollection)
{
    Directory.CreateDirectory(db.DatabaseName);

    Console.WriteLine($"Database: {db.DatabaseName}, Connection string: {db.ConnectionString}");
    var exporter = new SqliteToCsvExporter(db);
    exporter.ExportTablesToCsv();

    SqliteSchemaReporter.ReportTablesAndColumns(db.ConnectionString);

    SqliteCodeGenerator.GenerateDtoClasses(db.ConnectionString, db.DatabaseName);




}
Console.WriteLine("Press any key to end...");
Console.ReadKey();
