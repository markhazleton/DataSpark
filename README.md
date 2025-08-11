# 🗄️ SQL2CSV

[![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Build](https://img.shields.io/github/actions/workflow/status/markhazleton/sql2csv/ci.yml?branch=main)](https://github.com/markhazleton/sql2csv/actions)
[![Coverage](https://img.shields.io/badge/coverage-coming%20soon-lightgrey.svg)](#-testing--quality)

> .NET 9 toolkit for discovering SQLite databases, exporting tables to CSV, inspecting schema, and generating C# DTOs. Includes a console CLI and an early-stage web UI scaffold.

## ✨ Current Core Features (Implemented)

- 🔍 SQLite database discovery (directory scanning)
- � Export all tables of each discovered database to CSV (one file per table)
- 📊 Text-based schema reports (printed to console)
- 🏗️ Generate C# DTO record types (configurable namespace)
- 🧩 Reusable core services (discovery / export / schema / code generation)
- 🧪 115 passing tests (MSTest) – coverage publication pending
- ⚙️ Options pattern & DI-friendly architecture

> NOTE: Advanced web UI capabilities (drag & drop persistence dashboard, visual relationships, filtering UI, progress bars) are not yet implemented; see Roadmap.

### CLI Overview

| Command | Description | Key Options |
|---------|-------------|-------------|
| discover | List discovered SQLite databases in a directory | --path |
| export | Export all tables of each database to CSV | --path, --output |
| schema | Print schema reports | --path |
| generate | Generate C# DTO records | --path, --output, --namespace |

Planned (not yet implemented): per-table export selection, delimiter/header overrides, additional output formats (json, markdown), code template customization.

### CLI Usage Examples

```bash
# Discover databases
dotnet run --project sql2csv.console discover --path "C:\Data\DBs"

# Export databases to CSV
dotnet run --project sql2csv.console export --path "C:\Data\DBs" --output "C:\Exports"

# Schema reports
dotnet run --project sql2csv.console schema --path "C:\Data\DBs"

# Generate DTOs
dotnet run --project sql2csv.console generate --path "C:\Data\DBs" --output "C:\Gen" --namespace "MyApp.Models"
```

### Architecture Highlights

- Clean separation (Core library reused by console & web)
- DI + options pattern
- Structured logging via Microsoft.Extensions.Logging
- Focused services: discovery / export / schema / code generation
- Test project validating primary flows

### Web UI (Status: Early)

Web project scaffolds controllers, services and asset pipeline (Tailwind + Alpine.js). Advanced UI features listed earlier are pending implementation.

## 🚀 Quick Start

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- Windows, macOS, or Linux
- Node.js 18+ (for web application frontend build)

### Installation

```bash
# Clone the repository
git clone https://github.com/markhazleton/sql2csv.git
cd sql2csv

# Build the entire solution
dotnet build

# Install frontend dependencies (for web app)
cd sql2csv.web
npm install
npm run build
cd ..
```

### Run Web (early preview)

```bash
dotnet run --project sql2csv.web
# Browse http://localhost:5000
```

<!-- Verbose marketing demos removed. -->

**🎯 Console Features Showcase:**

- **Smart Discovery Engine**: Recursively finds databases with pattern matching
- **Parallel Processing**: Multi-threaded operations for maximum performance
- **Rich Progress Indicators**: Real-time progress with ETA calculations
- **Detailed Logging**: Configurable log levels with structured output
- **Error Recovery**: Graceful handling of corrupted or locked databases
- **Cross-Platform**: Identical functionality on Windows, macOS, and Linux

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

```text
sql2csv/
├── 🌐 sql2csv.web/               # Web Application
│   ├── Controllers/              # MVC controllers
│   ├── Models/                   # View models and DTOs
│   ├── Services/                 # Web-specific services
│   ├── Views/                    # Razor views and layouts
│   ├── wwwroot/                  # Static files (CSS, JS, images)
│   ├── Program.cs               # Web app entry point
│   └── appsettings.json         # Web app configuration
├── 📱 sql2csv.console/           # Console Application
│   ├── Presentation/             # CLI commands and UI logic
│   ├── Program.cs               # Console app entry point
│   └── appsettings.json         # Console app configuration
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

#### � Web Application

The web application provides an intuitive interface for database operations:

**Controllers:**

- `HomeController`: Main application flow, file upload, and analysis
- File management endpoints for CRUD operations

**Services:**

- `WebDatabaseService`: Web-specific database operations with timeout handling
- `PersistedFileService`: File persistence and metadata management

**Models:**

- `FileUploadViewModel`: File upload and selection interface
- `FileManagementViewModel`: Persisted file management
- `PersistedDatabaseFile`: File metadata and tracking

**Features:**

- **File Upload**: Drag-and-drop SQLite file upload with validation
- **File Persistence**: Save uploaded files with metadata for reuse
- **Interactive Analysis**: Real-time database schema browsing
- **Export & Generation**: Web-based CSV export and C# code generation

#### �🎯 Sql2Csv.Core Library

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

#### 🖥️ CLI Application

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

## 🧪 Testing & Quality

### 🎯 Comprehensive Test Coverage

Our commitment to quality is demonstrated through extensive testing infrastructure:

Current (Aug 2025):

- 115 passing tests (MSTest)
- Coverage badge pending (will replace placeholder when CI publishes)
- Benchmarks & extended static analysis planned

### 🏗️ Test Infrastructure Demos

**🔬 Unit Testing Excellence:**

```csharp
[TestMethod]
public async Task ExportTableToCsvAsync_WithValidTable_ShouldExportSuccessfully()
{
    // Arrange: Create test database with sample data
    var databaseConfig = new DatabaseConfiguration("TestDB", ConnectionString);
    var outputFilePath = Path.Combine(_outputDirectory, "users_export.csv");

    // Act: Export table to CSV
    var result = await _exportService.ExportTableToCsvAsync(databaseConfig, "Users", outputFilePath);

    // Assert: Verify successful export with real data validation
    result.IsSuccess.Should().BeTrue();
    result.RowCount.Should().Be(3);
    
    var csvContent = await File.ReadAllTextAsync(outputFilePath);
    csvContent.Should().Contain("John Doe");
    csvContent.Should().Contain("jane@example.com");
}
```

**⚡ Integration Testing:**

- **End-to-End Workflows**: Full database discovery → analysis → export → code generation
- **Real Database Testing**: Tests use actual SQLite databases, not mocks
- **Cross-Platform Validation**: Automated testing on Windows, macOS, and Linux
- **Performance Testing**: Benchmarks for large database processing

**🔧 Test Infrastructure Features:**

- **Fluent Assertions**: Beautiful, readable test assertions
- **Test Data Management**: Automated test database creation and cleanup
- **Parallel Execution**: Fast test execution with proper isolation
- **Mock Objects**: Strategic mocking with Moq for external dependencies

### �️ Quality Gates & Standards

**Code Quality Metrics:**

- **Static Analysis**: Comprehensive code analysis with industry-standard rules
- **Dependency Scanning**: Automated vulnerability scanning of all dependencies
- **Code Style**: Consistent formatting and naming conventions
- **Documentation Coverage**: XML documentation for all public APIs

**🎯 Quality Standards:**

- **SOLID Principles**: Clean Architecture implementation
- **Nullable Reference Types**: Modern C# safety features
- **Async/Await Best Practices**: Proper asynchronous programming patterns
- **Error Handling**: Comprehensive exception handling and logging

### 🔍 Demo Test Scenarios

#### Scenario 1: Large Database Processing

```csharp
[TestMethod]
public async Task ExportDatabasesAsync_WithLargeDatabase_ShouldMaintainPerformance()
{
    // Tests processing of databases with 100,000+ records
    // Validates memory usage and processing time
    // Ensures consistent performance across different database sizes
}
```

#### Scenario 2: Concurrent Operations

```csharp
[TestMethod]
public async Task ParallelExport_WithMultipleDatabases_ShouldNotInterfere()
{
    // Tests simultaneous export of multiple databases
    // Validates thread safety and resource management
    // Ensures no data corruption or race conditions
}
```

#### Scenario 3: Error Recovery

```csharp
[TestMethod]
public async Task ExportTableToCsvAsync_WithCorruptedDatabase_ShouldHandleGracefully()
{
    // Tests behavior with corrupted or locked database files
    // Validates error reporting and recovery mechanisms
    // Ensures system stability under adverse conditions
}
```

Further coverage & benchmark reporting will be integrated via CI.

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

## 📊 Sample Output

### 🎬 Live CSV Export Demo

Our advanced export engine creates perfectly formatted CSV files with rich metadata:

```csv
# Example: Users table export (Users_extract.csv)
Id,Name,Email,Age,CreatedDate
1,"John Doe","john@example.com",30,"2024-01-15T10:30:00Z"
2,"Jane Smith","jane@example.com",25,"2024-01-16T14:22:00Z"
3,"Bob Johnson","bob@example.com",35,"2024-01-17T09:15:00Z"
```

**Advanced Export Features:**

- Creates organized directory structure: `{DatabaseName}/{TableName}_extract.csv`
- Configurable headers, delimiters (`,`, `;`, `|`, `\t`)
- Proper CSV escaping with CsvHelper for complex data
- Custom encoding support (UTF-8, UTF-16, ASCII)
- Batch processing with progress tracking
- Error handling with detailed failure reports

### 🏗️ Generated C# Code Showcase

Experience our intelligent code generation that creates modern, documented C# classes:

```csharp
namespace MyCompany.Data.Models;

/// <summary>
/// Data Transfer Object for Users table
/// Generated on 2024-01-15 at 10:30:00 UTC
/// </summary>
public record User
{
    /// <summary>
    /// Primary key identifier (INTEGER, NOT NULL)
    /// </summary>
    public int Id { get; init; }
    
    /// <summary>
    /// User's full name (TEXT, NOT NULL)
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// User's email address (TEXT, NULLABLE)
    /// </summary>
    public string? Email { get; init; }
    
    /// <summary>
    /// User's age in years (INTEGER, NULLABLE)
    /// </summary>
    public int? Age { get; init; }
    
    /// <summary>
    /// Record creation timestamp (TEXT, DEFAULT: CURRENT_TIMESTAMP)
    /// </summary>
    public string? CreatedDate { get; init; }
}

/// <summary>
/// Data Transfer Object for Orders table
/// Foreign Key: UserId references Users.Id
/// </summary>
public record Order
{
    public int Id { get; init; }
    public int? UserId { get; init; }
    public double? Amount { get; init; }
    public string? OrderDate { get; init; }
    
    /// <summary>
    /// Navigation property to related User
    /// </summary>
    public User? User { get; init; }
}
```

**🎨 Code Generation Features:**

- **Modern C# Patterns**: Records, nullable reference types, init-only properties
- **Rich Documentation**: XML comments with data types and constraints
- **Intelligent Naming**: PascalCase conversion from database naming conventions
- **Relationship Mapping**: Foreign key detection with navigation properties
- **Flexible Output**: Support for classes, records, and interfaces
- **Custom Templates**: Configurable code generation templates

### 📋 Comprehensive Schema Reports

Our schema analysis provides detailed insights into database structure:

```text
========================================
DATABASE SCHEMA REPORT
========================================
Database: MyProject.db
Generated: 2024-01-15 10:30:00 UTC
Total Tables: 3
Total Columns: 15
========================================

TABLE: Users (main)
  Rows: 1,247
  Columns: 5
  Primary Key: Id (INTEGER)
  
  COLUMNS:
  ├─ Id          │ INTEGER │ PK │ NOT NULL │ AUTOINCREMENT
  ├─ Name        │ TEXT    │    │ NOT NULL │
  ├─ Email       │ TEXT    │    │ NULL     │ UNIQUE
  ├─ Age         │ INTEGER │    │ NULL     │
  └─ CreatedDate │ TEXT    │    │ NULL     │ DEFAULT: CURRENT_TIMESTAMP

TABLE: Orders (main)
  Rows: 3,891
  Columns: 4
  Primary Key: Id (INTEGER)
  Foreign Keys: UserId → Users.Id
  
  COLUMNS:
  ├─ Id        │ INTEGER │ PK │ NOT NULL │ AUTOINCREMENT
  ├─ UserId    │ INTEGER │ FK │ NULL     │ → Users.Id
  ├─ Amount    │ REAL    │    │ NULL     │
  └─ OrderDate │ TEXT    │    │ NULL     │ DEFAULT: CURRENT_TIMESTAMP

TABLE: Products (main)
  Rows: 156
  Columns: 4
  Primary Key: Id (INTEGER)
  
  COLUMNS:
  ├─ Id          │ INTEGER │ PK │ NOT NULL │ AUTOINCREMENT
  ├─ Name        │ TEXT    │    │ NOT NULL │
  ├─ Price       │ REAL    │    │ NULL     │
  └─ Description │ TEXT    │    │ NULL     │

========================================
RELATIONSHIPS:
Orders.UserId → Users.Id (Many-to-One)

INDEXES:
Users.Email (UNIQUE)

STATISTICS:
Total Records: 5,294
Average Records per Table: 1,765
Largest Table: Orders (3,891 rows)
========================================
```

**📊 Schema Analysis Features:**

- **Visual Tree Structure**: Beautiful ASCII art representation
- **Relationship Mapping**: Foreign key detection and visualization
- **Statistical Analysis**: Row counts, data distribution, and table sizes
- **Index Information**: Index types and performance implications
- **Data Type Analysis**: Type mapping and nullable constraints
- **Export Options**: JSON, XML, and markdown format support

## 🎯 Typical Use Cases

### 🔍 Interactive Database Analysis & Development

**🎬 Demo Scenario**: A development team needs to quickly understand a legacy SQLite database structure.

- **Instant Database Upload**: Drag the database file into the web interface
- **Live Schema Exploration**: Browse all tables, columns, and relationships interactively
- **Data Sampling**: View actual data with filtering and search capabilities
- **Documentation Generation**: Export schema reports for team documentation
- **Code Integration**: Generate C# models for immediate use in applications

**Real Benefits**: Teams save hours of manual database exploration and documentation work.

### 🚚 Enterprise Data Migration Projects

**🎬 Demo Scenario**: Migrating from legacy SQLite databases to modern SQL Server.

```bash
# Discover all databases across multiple directories
dotnet run discover --path "C:\LegacyData" --recursive true

# Export all data to CSV for analysis and migration scripts
dotnet run export --path "C:\LegacyData" --output "C:\Migration\CSVs" --delimiter "|"

# Generate C# models for the new application layer
dotnet run generate --path "C:\LegacyData" --namespace "NewApp.Entities" --type "record"
```

**Real Benefits**: Automated discovery and export of hundreds of databases, saving weeks of manual work.

### 🏗️ Modern Code Generation for APIs

**🎬 Demo Scenario**: Building REST APIs that need DTOs matching existing database schemas.

- **Bulk Code Generation**: Generate DTOs for entire database collections
- **Modern C# Features**: Records, nullable reference types, and init-only properties
- **Custom Namespaces**: Organize generated code into proper project structure
- **Documentation**: Rich XML comments with database metadata

**Real Benefits**: Consistent, well-documented DTOs generated in minutes instead of hours of manual coding.

### 📊 Business Intelligence & Data Analysis

**🎬 Demo Scenario**: Analysts need CSV exports for Excel/Power BI reporting.

- **Selective Export**: Choose specific tables through the web interface
- **Custom Formatting**: Configure delimiters and encoding for target systems
- **Batch Processing**: Export multiple databases simultaneously
- **Progress Tracking**: Real-time progress for large datasets

**Real Benefits**: Self-service data extraction without IT intervention, enabling faster business insights.

### 🔄 Database Backup & Archival

**🎬 Demo Scenario**: Creating human-readable backups of critical SQLite databases.

- **Automated Discovery**: Find all databases in backup directories
- **Comprehensive Export**: Export all tables to organized CSV structure
- **Schema Documentation**: Generate reports for archive documentation
- **Verification**: Built-in validation ensures complete data export

**Real Benefits**: Reliable, auditable backups that can be restored without specialized tools.

### 👥 Team Collaboration & Documentation

**🎬 Demo Scenario**: Sharing database analysis across development teams.

- **File Persistence**: Save analyzed databases for team access
- **Metadata Tracking**: Description, tags, and access history
- **Schema Sharing**: Export schema reports in multiple formats
- **Code Sharing**: Generate and distribute consistent DTOs

**Real Benefits**: Centralized database knowledge base that improves team productivity and reduces duplicate work.

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

### Next

- CI workflow (build + test + coverage publishing)
- Coverage badge replacement
- Export command delimiter & headers options
- Schema report alternative formats (json, markdown)

### Planned

- Enhanced web UI (drag & drop upload, multi-table export)
- Per-table selection for export & generation
- Docker packaging
- Benchmark suite (BenchmarkDotNet)

### Backlog

- REST API + OpenAPI
- Additional database providers (PostgreSQL / MySQL / SQL Server)
- Plugin architecture (custom exporters / code templates)
- Advanced export formats (JSON, Parquet, Excel)
- Auth & RBAC (web/API)
- Internationalization

Issue links will be added as items move forward—contributions welcome.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙋‍♂️ Support

- 📧 **Issues**: [GitHub Issues](https://github.com/markhazleton/sql2csv/issues)
- 💬 **Discussions**: [GitHub Discussions](https://github.com/markhazleton/sql2csv/discussions)
- 📖 **Documentation**: [Wiki](https://github.com/markhazleton/sql2csv/wiki)

## ⭐ Show Your Support

Give a ⭐️ if this project helped you!

---

Made with ❤️ by [Mark Hazleton](https://github.com/markhazleton)
