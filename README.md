# sql2csv

A console appliation for Sqlite datbase that runs a set of SQL scripts and save the output to CSV files

# SQLite Database Utilities

This repository contains utility classes for working with SQLite databases. These utilities facilitate the extraction of data from SQLite databases and allow for the conversion of this data into other formats or databases, such as CSV files and MS Access databases.

## Utilities

### 1. SQLite to CSV Exporter

The `SQLiteToCsvExporter` class connects to a specified SQLite database file, iterates over all the tables within the database, and exports the data from each table to a separate CSV file named after the table.

#### Usage

To use the `SQLiteToCsvExporter`, create an instance of the class with the path to your SQLite database file, and then call the `ExportTablesToCsv` method.

```csharp
var exporter = new SQLiteToCsvExporter("path/to/your/database.db");
exporter.ExportTablesToCsv();
```

