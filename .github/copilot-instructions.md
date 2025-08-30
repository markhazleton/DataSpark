# GitHub Copilot Instructions for SQL2CSV

## Project Overview

SQL2CSV is a comprehensive .NET 9 toolkit for SQLite database operations including:

- **Console Application** (`sql2csv.console/`) - CLI for database discovery, CSV export, schema analysis, and C# DTO generation
- **Web Application** (`sql2csv.web/`) - Web UI with drag-and-drop upload, interactive analysis, and export capabilities
- **Core Library** (`Sql2Csv.Core/`) - Reusable business logic services following Clean Architecture
- **Test Suite** (`Sql2Csv.Tests/`) - Comprehensive unit and integration tests (115+ tests)
- **Benchmarks** (`Sql2Csv.Benchmarks/`) - Performance testing with BenchmarkDotNet

## Development Environment & Running the Application

### Running the Web Application

**ALWAYS use port 5001 for the web application** to avoid conflicts:

```bash
# Navigate to the web project directory
cd sql2csv.web

# Run the application on port 5001
dotnet run --urls=http://localhost:5001
```

**Alternative method using project specification:**

```bash
# From the solution root directory
dotnet run --project sql2csv.web --urls=http://localhost:5001
```

The web application will be available at: `http://localhost:5001`

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test Sql2Csv.Tests
```

### Running Benchmarks

```bash
cd Sql2Csv.Benchmarks
dotnet run -c Release
```

### Terminal Management Guidelines

**IMPORTANT: Minimize terminal usage and reuse existing sessions**

- **DO NOT** create new terminals unnecessarily
- **ALWAYS** check for existing terminal sessions first using `get_terminal_output`
- **REUSE** existing terminals by checking their current working directory
- **USE** `run_in_terminal` with `isBackground=true` only for long-running processes (like web servers)
- **USE** `run_in_terminal` with `isBackground=false` for quick commands that need immediate output
- **NAVIGATE** within existing terminals using `cd` commands rather than spawning new ones

**Example of proper terminal reuse:**

```bash
# Check existing terminal first
# If in wrong directory, navigate: cd "c:\GitHub\MarkHazleton\sql2csv\sql2csv.web"
# Then run: dotnet run --urls=http://localhost:5001
```

## Coding Standards & Architecture

### Clean Architecture Principles

- **Domain Logic**: Keep business rules in `Sql2Csv.Core/Services/`
- **Separation of Concerns**: Console and Web projects should only contain presentation logic
- **Dependency Injection**: Use Microsoft.Extensions.DependencyInjection throughout
- **Configuration**: Follow the Options pattern with strongly-typed configuration classes

### Code Quality Standards

- **Nullable Reference Types**: Always enabled - use `?` for nullable types, never return null without explicit nullable annotation
- **Modern C# Features**: Prefer records over classes for DTOs, use init-only properties, pattern matching
- **Async/Await**: All I/O operations must be async with proper ConfigureAwait(false) in library code
- **Error Handling**: Use Result pattern or proper exception handling with detailed logging
- **XML Documentation**: Required for all public APIs with `<summary>`, `<param>`, and `<returns>` tags

### Testing Requirements

- **Test Coverage**: Maintain high test coverage - current target is 85%+
- **Test Categories**: Separate unit tests, integration tests, and benchmarks
- **Test Naming**: Use descriptive names: `Method_Scenario_ExpectedBehavior`
- **Arrange-Act-Assert**: Clear test structure with proper setup and assertions
- **Mock External Dependencies**: Use Moq for external services, test with real SQLite databases where appropriate

## Project-Specific Guidelines

### Sql2Csv.Core Library

```csharp
// Preferred service interface pattern
public interface IDatabaseDiscoveryService
{
    Task<IEnumerable<DatabaseInfo>> DiscoverDatabasesAsync(string path, CancellationToken cancellationToken = default);
}

// Preferred implementation pattern with proper logging and error handling
public class DatabaseDiscoveryService : IDatabaseDiscoveryService
{
    private readonly ILogger<DatabaseDiscoveryService> _logger;
    private readonly IOptions<Sql2CsvOptions> _options;

    public DatabaseDiscoveryService(ILogger<DatabaseDiscoveryService> logger, IOptions<Sql2CsvOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<IEnumerable<DatabaseInfo>> DiscoverDatabasesAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        _logger.LogInformation("Starting database discovery in path: {Path}", path);

        try
        {
            // Implementation with proper async/await
            var databases = new List<DatabaseInfo>();
            // ... discovery logic

            _logger.LogInformation("Discovered {Count} databases", databases.Count);
            return databases;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover databases in path: {Path}", path);
            throw;
        }
    }
}
```

### Configuration Pattern

```csharp
// Strongly-typed configuration
public class Sql2CsvOptions
{
    public const string SectionName = "Sql2Csv";

    public string RootPath { get; set; } = string.Empty;
    public PathOptions Paths { get; set; } = new();
    public DatabaseOptions Database { get; set; } = new();
    public ExportOptions Export { get; set; } = new();
}

public class ExportOptions
{
    public bool IncludeHeaders { get; set; } = true;
    public string Delimiter { get; set; } = ",";
    public string Encoding { get; set; } = "UTF-8";
}
```

### Console Application Patterns

- Use `Microsoft.Extensions.Hosting` for dependency injection and configuration
- Implement commands using Command pattern with clear separation
- All output should go through `ILogger` for consistency
- Support `--help` and proper exit codes

### Web Application Patterns

- Follow ASP.NET Core MVC conventions
- Use ViewModels for all views - never pass domain models directly
- Implement proper model validation with Data Annotations
- Use TempData for user messages and maintain state properly
- File uploads should validate file type and size

### Database Patterns

```csharp
// Preferred database access pattern
public async Task<IEnumerable<TableInfo>> GetTablesAsync(string connectionString, CancellationToken cancellationToken = default)
{
    using var connection = new SqliteConnection(connectionString);
    await connection.OpenAsync(cancellationToken);

    const string sql = @"
        SELECT name, type, sql
        FROM sqlite_master
        WHERE type = 'table' AND name NOT LIKE 'sqlite_%'
        ORDER BY name";

    using var command = new SqliteCommand(sql, connection);
    using var reader = await command.ExecuteReaderAsync(cancellationToken);

    var tables = new List<TableInfo>();
    while (await reader.ReadAsync(cancellationToken))
    {
        tables.Add(new TableInfo
        {
            Name = reader.GetString("name"),
            Type = reader.GetString("type"),
            CreateSql = reader.IsDBNull("sql") ? null : reader.GetString("sql")
        });
    }

    return tables;
}
```

## File Organization

### Session Documentation

- **All Copilot-generated markdown files** must be placed in `/copilot/session-{YYYY-MM-DD}/`
- Use descriptive filenames: `feature-implementation-notes.md`, `refactoring-plan.md`, `troubleshooting-guide.md`
- Include session metadata at the top of each file:

  ```markdown
  # Session: [Title]

  **Date**: 2025-08-30
  **Focus**: [Brief description]
  **Files Modified**: [List of files]
  ```

### Terminal Session Management

**CRITICAL: Follow these terminal usage guidelines to avoid resource waste and confusion:**

1. **Check Before Creating**: Always use `get_terminal_output` to check existing terminals before creating new ones
2. **Reuse Existing**: Navigate within existing terminals using `cd` commands rather than spawning new sessions
3. **Background vs Foreground**:
   - Use `isBackground=true` ONLY for long-running servers (like `dotnet run`)
   - Use `isBackground=false` for commands that need immediate output
4. **Port Consistency**: Always use port 5001 for the web application: `--urls=http://localhost:5001`
5. **Working Directory**: Check and navigate to correct directories rather than creating new terminals

**Example Terminal Workflow:**

```bash
# 1. Check existing terminals first
# 2. If terminal exists, navigate: cd "c:\GitHub\MarkHazleton\sql2csv\sql2csv.web"
# 3. Then run: dotnet run --urls=http://localhost:5001
# 4. Use get_terminal_output to monitor running processes
```

### Project Structure Respect

- Keep console-specific code in `sql2csv.console/`
- Keep web-specific code in `sql2csv.web/`
- Shared business logic belongs in `Sql2Csv.Core/`
- All tests go in `Sql2Csv.Tests/` with appropriate subdirectories
- Configuration files should be project-specific unless shared

## Performance & Security

### Performance Guidelines

- Use `IAsyncEnumerable<T>` for streaming large datasets
- Implement proper cancellation token support
- Use connection pooling and proper disposal patterns
- Consider memory usage for large file operations
- Profile with BenchmarkDotNet for critical paths

### Security Considerations

- Validate all file uploads (type, size, content)
- Use parameterized queries - never string concatenation
- Sanitize file paths to prevent directory traversal
- Log security-relevant events appropriately
- Follow OWASP guidelines for web application security

## Common Patterns to Follow

### Result Pattern for Service Operations

```csharp
public record Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    public Exception? Exception { get; init; }

    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(string error, Exception? exception = null) =>
        new() { IsSuccess = false, Error = error, Exception = exception };
}
```

### Logging Standards

```csharp
// Use structured logging with consistent message templates
_logger.LogInformation("Exporting table {TableName} from database {DatabasePath} to {OutputPath}",
    tableName, databasePath, outputPath);

// Log performance-critical operations
using var activity = _logger.BeginScope("ExportOperation");
var stopwatch = Stopwatch.StartNew();
// ... operation
_logger.LogInformation("Export completed in {ElapsedMs}ms for {RowCount} rows",
    stopwatch.ElapsedMilliseconds, rowCount);
```

### Extension Methods

- Place in dedicated `Extensions/` folders
- Use meaningful names and XML documentation
- Follow .NET naming conventions
- Keep them focused and reusable

## Testing Patterns

### Unit Test Structure

```csharp
[TestClass]
public class ExportServiceTests
{
    private readonly Mock<ILogger<ExportService>> _mockLogger;
    private readonly IOptions<Sql2CsvOptions> _options;
    private readonly ExportService _exportService;
    private readonly string _testDatabasePath;
    private readonly string _outputDirectory;

    public ExportServiceTests()
    {
        _mockLogger = new Mock<ILogger<ExportService>>();
        _options = Options.Create(new Sql2CsvOptions
        {
            Export = new ExportOptions { IncludeHeaders = true, Delimiter = "," }
        });
        _exportService = new ExportService(_mockLogger.Object, _options);
        _testDatabasePath = CreateTestDatabase();
        _outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_outputDirectory);
    }

    [TestMethod]
    public async Task ExportTableToCsvAsync_WithValidTable_ShouldExportSuccessfully()
    {
        // Arrange
        var tableName = "Users";
        var outputPath = Path.Combine(_outputDirectory, "users.csv");

        // Act
        var result = await _exportService.ExportTableToCsvAsync(_testDatabasePath, tableName, outputPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        File.Exists(outputPath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("Id,Name,Email"); // Headers
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_outputDirectory))
            Directory.Delete(_outputDirectory, true);
    }
}
```

## Dependencies & Libraries

### Approved Dependencies

- **Microsoft.Extensions.\*** - Dependency injection, logging, configuration, hosting
- **Microsoft.Data.Sqlite** - SQLite database access
- **CsvHelper** - CSV file operations
- **FluentAssertions** - Test assertions
- **Moq** - Mocking framework
- **BenchmarkDotNet** - Performance testing

### Adding New Dependencies

- Justify the need and evaluate alternatives
- Ensure compatibility with .NET 9
- Check license compatibility (prefer MIT/Apache)
- Update documentation and tests accordingly

## Git & Deployment

### Commit Message Format

```
type(scope): description

feat(export): add support for custom delimiters
fix(web): resolve file upload validation issue
test(core): add integration tests for schema service
docs(readme): update installation instructions
```

### Branch Naming

- `feature/description` - New features
- `fix/description` - Bug fixes
- `refactor/description` - Code refactoring
- `test/description` - Test improvements

### CI/CD Considerations

- All tests must pass before merge
- Maintain code coverage standards
- Update version numbers appropriately
- Ensure cross-platform compatibility

---

**Remember**: All Copilot-generated documentation and session notes must be organized in `/copilot/session-{YYYY-MM-DD}/` directories for proper project organization and knowledge management.
