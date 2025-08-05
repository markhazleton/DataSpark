# 🗄️ SQL2CSV

[![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)]()

> A powerful, modern .NET 9 console application and library for exporting SQLite databases to CSV format with advanced features including code generation and schema reporting.

## ✨ Features

- 🔍 **Database Discovery**: Automatically discovers SQLite databases in specified directories
- 📊 **CSV Export**: High-performance export of all tables to CSV format with CsvHelper
- 📋 **Schema Reporting**: Generates detailed schema reports for databases
- 🏗️ **Code Generation**: Generates modern C# DTO classes from database schemas
- ⚡ **Async/Await**: Fully asynchronous operations for optimal performance
- 🏛️ **Clean Architecture**: Modular design with proper separation of concerns
- 💉 **Dependency Injection**: Uses Microsoft.Extensions.DependencyInjection
- 📝 **Structured Logging**: Comprehensive logging with Microsoft.Extensions.Logging
- ⚙️ **Configuration**: JSON-based configuration with strongly-typed options
- 🖥️ **Modern CLI**: Feature-rich command-line interface using System.CommandLine

## 🚀 Quick Start

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- Windows, macOS, or Linux

### Installation

```bash
# Clone the repository
git clone https://github.com/markhazleton/sql2csv.git
cd sql2csv

# Build the solution
dotnet build

# Run the application
dotnet run --project sql2csv.console
```

### Basic Usage

```bash
# Export all databases to CSV
dotnet run --project sql2csv.console export --path "C:\Data\Databases" --output "C:\Data\Export"

# Generate schema reports
dotnet run --project sql2csv.console schema --path "C:\Data\Databases"

# Generate C# DTO classes
dotnet run --project sql2csv.console generate --path "C:\Data\Databases" --output "C:\Code\Generated"
```

## 📖 Documentation

### Commands

#### 🔄 Export Command

Exports all tables from discovered databases to CSV files:

```bash
dotnet run export [options]
```

**Options:**

- `--path`: Directory containing SQLite database files (default: `%LOCALAPPDATA%\SQL2CSV\data`)
- `--output`: Output directory for CSV files (default: `%LOCALAPPDATA%\SQL2CSV\export`)

**Example:**

```bash
dotnet run export --path "C:\MyDatabases" --output "C:\MyExports"
```

#### 📊 Schema Command

Generates detailed schema reports for all discovered databases:

```bash
dotnet run schema [options]
```

**Options:**

- `--path`: Directory containing SQLite database files

**Example:**

```bash
dotnet run schema --path "C:\ProjectDatabases"
```

#### 🏗️ Generate Command

Generates modern C# DTO classes from database schemas:

```bash
dotnet run generate [options]
```

**Options:**

- `--path`: Directory containing SQLite database files
- `--output`: Output directory for generated code (default: `%LOCALAPPDATA%\SQL2CSV\generated`)
- `--namespace`: Namespace for generated classes (default: `Sql2Csv.Generated`)

**Example:**

```bash
dotnet run generate --path "C:\Data" --output "C:\Code\Models" --namespace "MyCompany.Data.Models"
```

### Configuration

The application uses `appsettings.json` for configuration:

```json
{
  "Sql2Csv": {
    "RootPath": "C:\\Temp\\SQL2CSV",
    "Paths": {
      "Config": "config",
      "Data": "data", 
      "Scripts": "scripts"
    },
    "Database": {
      "DefaultName": "test.db",
      "Timeout": 600
    },
    "Export": {
      "IncludeHeaders": true,
      "Delimiter": ",",
      "Encoding": "UTF-8"
    }
  }
}
```

## 🏗️ Project Structure

This solution follows Clean Architecture principles with clear separation of concerns:

```
sql2csv/
├── 📱 sql2csv.console/           # Console Application
│   ├── Presentation/             # CLI commands and UI logic
│   ├── Program.cs               # Application entry point
│   └── appsettings.json         # Configuration
├── 📚 Sql2Csv.Core/             # Core Library
│   ├── Configuration/           # Configuration models
│   ├── Interfaces/              # Service contracts  
│   ├── Models/                  # Domain entities
│   └── Services/                # Business logic services
├── 🧪 Sql2Csv.Tests/            # Test Project
│   └── UnitTest1.cs            # Sample test
├── 📄 README.md                 # This file
└── 🔧 sql2csv.sln              # Solution file
```

### Core Components

#### 🎯 Sql2Csv.Core Library

The core library contains all business logic and can be referenced by multiple projects:

**Services:**

- `ApplicationService`: Main orchestration service
- `DatabaseDiscoveryService`: Discovers and validates SQLite database files
- `ExportService`: High-performance CSV export with rich metadata
- `SchemaService`: Database schema introspection and reporting
- `CodeGenerationService`: Template-based C# code generation

**Models:**

- `DatabaseConfiguration`: Database connection settings
- `TableInfo`, `ColumnInfo`: Schema information models
- Domain entities for database operations

**Interfaces:**

- `IDatabaseDiscoveryService`: Database discovery contract
- `IExportService`: CSV export contract
- `ISchemaService`: Schema operations contract
- `ICodeGenerationService`: Code generation contract

#### 🖥️ Console Application

The console application provides a modern CLI interface:

- **Command Factory**: Creates and configures CLI commands
- **Dependency Injection**: Configures services and logging
- **Configuration**: Loads settings from appsettings.json
- **Error Handling**: Comprehensive error handling and user feedback

### Architecture Benefits

- **🔧 Maintainability**: Clear separation makes code easy to understand and modify
- **🧪 Testability**: Each layer can be unit tested independently
- **🔄 Flexibility**: Business logic is independent of external frameworks
- **📈 Scalability**: Easy to add new features without affecting existing functionality
- **♻️ Reusability**: Core library can be used in web apps, APIs, or other applications

## 💻 Development

### Building

```bash
# Build the entire solution
dotnet build

# Build specific project
dotnet build sql2csv.console/Sql2Csv.csproj
```

### Testing

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Publishing

```bash
# Publish for Windows x64
dotnet publish sql2csv.console/Sql2Csv.csproj -c Release -r win-x64 --self-contained

# Publish for Linux x64
dotnet publish sql2csv.console/Sql2Csv.csproj -c Release -r linux-x64 --self-contained

# Publish for macOS x64
dotnet publish sql2csv.console/Sql2Csv.csproj -c Release -r osx-x64 --self-contained
```

## 📊 Output Examples

### CSV Export

- Creates a directory structure: `{DatabaseName}/{TableName}_extract.csv`
- Includes column headers (configurable)
- Properly escapes CSV values using CsvHelper
- Supports custom delimiters and encoding

### Generated C# DTOs

```csharp
namespace MyCompany.Data.Models;

/// <summary>
/// Data Transfer Object for Users table
/// </summary>
public record User
{
    /// <summary>
    /// Primary key identifier
    /// </summary>
    public int Id { get; init; }
    
    /// <summary>
    /// User's full name
    /// </summary>
    public string? Name { get; init; }
    
    /// <summary>
    /// User's email address
    /// </summary>
    public string? Email { get; init; }
}
```

### Schema Reports

- Detailed table and column information
- Data types and constraints
- Primary key and foreign key relationships
- Console-formatted output with rich formatting

## 🎯 Use Cases

### Data Migration

- **Legacy System Migration**: Export data from old SQLite databases
- **Database Consolidation**: Merge multiple SQLite databases
- **Platform Migration**: Move data to different database systems

### Code Generation

- **API Development**: Generate DTOs for web APIs
- **ORM Mapping**: Create entity classes for Entity Framework
- **Data Access Layer**: Generate strongly-typed data models

### Analysis & Reporting

- **Data Analysis**: Export to CSV for Excel/Power BI analysis
- **Backup & Archive**: Create human-readable backups
- **Documentation**: Generate schema documentation

## 🔧 Advanced Usage

### Using the Core Library

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sql2Csv.Core.Configuration;
using Sql2Csv.Core.Services;

// Setup dependency injection
var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());
services.Configure<Sql2CsvOptions>(config => 
{
    config.Export.IncludeHeaders = true;
    config.Export.Delimiter = ",";
});

// Register core services
services.AddScoped<IDatabaseDiscoveryService, DatabaseDiscoveryService>();
services.AddScoped<IExportService, ExportService>();
services.AddScoped<ISchemaService, SchemaService>();
services.AddScoped<ICodeGenerationService, CodeGenerationService>();
services.AddScoped<ApplicationService>();

var serviceProvider = services.BuildServiceProvider();

// Use the services
var app = serviceProvider.GetRequiredService<ApplicationService>();
await app.ExportDatabasesAsync("C:\\Data", "C:\\Export");
```

### Custom Configuration

```csharp
// Custom CSV export settings
var options = new Sql2CsvOptions
{
    Export = new ExportOptions
    {
        IncludeHeaders = false,
        Delimiter = "|",
        Encoding = "UTF-8"
    }
};
```

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

### Development Setup

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Make your changes and add tests
4. Ensure all tests pass: `dotnet test`
5. Commit your changes: `git commit -m 'Add amazing feature'`
6. Push to the branch: `git push origin feature/amazing-feature`
7. Open a Pull Request

### Code Style

- Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions)
- Use nullable reference types
- Include XML documentation for public APIs
- Write unit tests for new functionality

## 📋 Roadmap

- [ ] **Web API**: REST API for remote database operations
- [ ] **Docker Support**: Containerized deployment options
- [ ] **Azure Functions**: Serverless export capabilities
- [ ] **Multiple Database Support**: MySQL, PostgreSQL, SQL Server
- [ ] **Advanced Export Formats**: JSON, XML, Parquet
- [ ] **Schema Validation**: Compare schemas across databases
- [ ] **Performance Monitoring**: Built-in performance metrics
- [ ] **Plugin Architecture**: Extensible export formats

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙋‍♂️ Support

- 📧 **Issues**: [GitHub Issues](https://github.com/markhazleton/sql2csv/issues)
- 💬 **Discussions**: [GitHub Discussions](https://github.com/markhazleton/sql2csv/discussions)
- 📖 **Documentation**: [Wiki](https://github.com/markhazleton/sql2csv/wiki)

## ⭐ Show Your Support

Give a ⭐️ if this project helped you!

---

<div align="center">
  <p>Made with ❤️ by <a href="https://github.com/markhazleton">Mark Hazleton</a></p>
</div>
