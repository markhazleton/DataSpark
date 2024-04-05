# sql2csv

A console appliation for Sqlite datbase that runs a set of SQL scripts and save the output to CSV files

# SQLite Database Utilities

This repository contains utility classes for working with SQLite databases. These utilities facilitate the extraction of data from SQLite databases and allow for the conversion of this data into other formats or databases, such as CSV files and MS Access databases.

## Utilities

### SQLite to CSV Exporter
The `SQLiteToCsvExporter` class connects to a specified SQLite database file, iterates over all the tables within the database, and exports the data from each table to a separate CSV file named after the table.
A class that connects to a specified SQLite database, iterates over all the tables, and exports the data from each table to a CSV file named after the table. This utility is useful for data analysis, backup, or migration purposes.

### SQLite to MS Access Converter
A class that reads data from a SQLite database and creates a new MS Access database, copying all tables and their data into the new database. This utility aids in migrating data from SQLite to MS Access for users who prefer or require an MS Access database for their applications.

### SQLite DTO Generator
This utility generates Data Transfer Object (DTO) C# classes for each table in the SQLite database. Each DTO class includes properties that correspond to the columns of the table, facilitating easy data manipulation and transfer within C# applications.

### SQLite Service Class Generator
Building on the DTO classes, this utility generates a service class for each table in the SQLite database. Each service class includes methods for creating, reading, updating, and deleting records (CRUD operations), using the corresponding DTO class to pass data in and out of the service. This abstraction layer simplifies database interactions and enforces a separation of concerns within the application architecture.

These utilities are designed to provide a robust toolset for developers working with SQLite databases in C# environments, streamlining common database operations and enhancing productivity.

#### Usage

To use the `SQLiteToCsvExporter`, create an instance of the class with the path to your SQLite database file, and then call the `ExportTablesToCsv` method.

```csharp
var exporter = new SQLiteToCsvExporter("path/to/your/database.db");
exporter.ExportTablesToCsv();
```

