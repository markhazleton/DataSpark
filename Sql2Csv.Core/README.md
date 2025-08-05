# Sql2Csv.Core

This is the core library for the SQL to CSV conversion functionality. It contains the reusable business logic and can be referenced by multiple projects.

## Structure

- **Configuration**: Configuration classes and options
- **Interfaces**: Service interfaces and contracts
- **Models**: Data models and DTOs
- **Services**: Business logic services

## Services Included

- **ApplicationService**: Main orchestration service
- **DatabaseDiscoveryService**: Service for finding and validating database files
- **ExportService**: Service for exporting data to CSV format
- **SchemaService**: Service for database schema operations
- **CodeGenerationService**: Service for generating DTO classes

## Dependencies

- Microsoft.Data.Sqlite
- CsvHelper
- Microsoft.Extensions.* (Configuration, DependencyInjection, Logging, Options)

## Usage

Reference this library in your project and register the services in your DI container:

```csharp
services.AddScoped<IDatabaseDiscoveryService, DatabaseDiscoveryService>();
services.AddScoped<IExportService, ExportService>();
services.AddScoped<ISchemaService, SchemaService>();
services.AddScoped<ICodeGenerationService, CodeGenerationService>();
services.AddScoped<ApplicationService>();
```
