# SQL to CSV Exporter

A modern .NET 9 console application for exporting SQLite databases to CSV format with advanced features including code generation and schema reporting.

## Features

- **Database Discovery**: Automatically discovers SQLite databases in specified directories
- **CSV Export**: Exports all tables from discovered databases to CSV format
- **Schema Reporting**: Generates detailed schema reports for databases
- **Code Generation**: Generates C# DTO classes from database schemas
- **Modern CLI**: Command-line interface with comprehensive options
- **Async/Await**: Fully asynchronous operations for better performance
- **Dependency Injection**: Uses Microsoft.Extensions.DependencyInjection
- **Structured Logging**: Comprehensive logging with Microsoft.Extensions.Logging
- **Configuration**: JSON-based configuration with appsettings.json

## Requirements

- .NET 9.0 or later
- Windows, macOS, or Linux

## Installation

1. Clone the repository
2. Navigate to the project directory
3. Build the project:

   ```bash
   dotnet build
   ```

## Usage

The application supports three main commands:

### Export Command

Exports all tables from discovered databases to CSV files:

```bash
dotnet run export --path "C:\Data\Databases" --output "C:\Data\Export"
```

**Parameters:**

- `--path`: Directory containing SQLite database files (default: `%LOCALAPPDATA%\SQL2CSV\data`)
- `--output`: Output directory for CSV files (default: `%LOCALAPPDATA%\SQL2CSV\export`)

### Schema Command

Generates schema reports for all discovered databases:

```bash
dotnet run schema --path "C:\Data\Databases"
```

**Parameters:**

- `--path`: Directory containing SQLite database files

### Generate Command

Generates C# DTO classes from database schemas:

```bash
dotnet run generate --path "C:\Data\Databases" --output "C:\Code\Generated" --namespace "MyProject.Models"
```

**Parameters:**

- `--path`: Directory containing SQLite database files
- `--output`: Output directory for generated code
- `--namespace`: Namespace for generated classes (default: `Sql2Csv.Generated`)

## Configuration

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

## Architecture

This application follows Clean Architecture principles with clear separation of concerns:

```text
Sql2Csv/
├── Core/                          # Domain layer
│   ├── Configuration/             # Configuration models
│   ├── Interfaces/               # Service contracts
│   └── Models/                   # Domain entities
├── Infrastructure/               # Infrastructure layer
│   └── Services/                # External service implementations
├── Application/                  # Application layer
│   └── Services/                # Application service orchestration
├── Presentation/                 # Presentation layer
│   └── Commands/                # CLI command definitions
└── Legacy/                       # Legacy code (deprecated)
    ├── Models/                  # Old configuration models
    └── Services/                # Old service implementations
```

### Architecture Principles

- **Clean Architecture**: Strict separation of concerns with proper layer boundaries
- **Dependency Injection**: All services are registered in the DI container
- **Async Programming**: Full async/await support for I/O operations
- **Error Handling**: Comprehensive error handling and logging
- **Configuration**: Strongly-typed configuration with options pattern
- **CLI Framework**: Uses System.CommandLine for robust CLI experience

### Core Layer

- **Models**: Domain entities like `DatabaseConfiguration`, `TableInfo`, `ColumnInfo`
- **Interfaces**: Service contracts defining business operations
- **Configuration**: Strongly-typed configuration models

### Infrastructure Layer

- **Services**: Concrete implementations of core interfaces
- Database access, file I/O, external service integrations

### Application Layer

- **Services**: Application service orchestration
- Business logic coordination and workflow management

### Presentation Layer

- **Commands**: CLI command definitions and handlers
- User interface concerns and input validation

### Key Services

#### Core Interfaces

- `IDatabaseDiscoveryService`: Discovers SQLite databases in directories
- `IExportService`: Exports database tables to CSV format with rich result metadata
- `ISchemaService`: Provides database schema information and reporting
- `ICodeGenerationService`: Generates C# DTO classes from schemas

#### Infrastructure Services

- `DatabaseDiscoveryService`: File system-based database discovery
- `ExportService`: High-performance CSV export with CsvHelper
- `SchemaService`: SQLite schema introspection and reporting
- `CodeGenerationService`: Template-based C# code generation

#### Application Services

- `ApplicationService`: Orchestrates operations across multiple databases

## Output

### CSV Export

- Creates a directory for each database
- Exports each table as `{TableName}_extract.csv`
- Includes column headers (configurable)
- Properly escapes CSV values

### Code Generation

- Generates modern C# record types
- Proper nullable reference types
- Pascal case property names
- XML documentation comments
- Handles SQLite to C# type mapping

### Schema Reports

- Detailed table and column information
- Data types and constraints
- Primary key indicators
- Console-formatted output

## Examples

### Basic Export

```bash
# Export all databases in the default data directory
dotnet run export

# Export with custom paths
dotnet run export --path "C:\MyDatabases" --output "C:\MyExports"
```

### Generate DTOs

```bash
# Generate DTOs with custom namespace
dotnet run generate --namespace "MyCompany.Data.Models"
```

### Schema Analysis

```bash
# View schema for all databases
dotnet run schema --path "C:\ProjectDatabases"
```

## Development

### Architecture Benefits

The Clean Architecture implementation provides:

- **Maintainability**: Clear separation of concerns makes code easier to understand and modify
- **Testability**: Each layer can be unit tested independently with proper mocking
- **Flexibility**: Business logic is independent of external frameworks and databases
- **Scalability**: Easy to add new features without affecting existing functionality

### Performance Features

- **Async I/O**: All database and file operations are asynchronous
- **Parallel Processing**: Multiple tables can be exported concurrently
- **Memory Efficient**: Streaming-based processing for large datasets
- **Progress Tracking**: Detailed logging and metrics collection

### Building

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Publishing

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

### Code Quality

- **Nullable Reference Types**: Enhanced null safety throughout codebase
- **Modern C# Features**: Records, pattern matching, and top-level statements
- **Dependency Injection**: Proper service lifetime management
- **Configuration**: Strongly-typed options with validation

## License

This project is licensed under the MIT License.
