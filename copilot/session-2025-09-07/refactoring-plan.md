# Refactoring Plan: Move Services and Models from sql2csv.web to Sql2Csv.Core

**Date**: 2025-09-07  
**Focus**: Reduce web project size by moving business logic to core library  
**Files Modified**: Multiple services and models files

## Analysis of Current Structure

### Services to Move to Core

1. **PersistedFileService.cs** → Move to `Sql2Csv.Core/Services/`
   - Business logic for file persistence
   - No web-specific dependencies except IFormFile (will need interface abstraction)

2. **WebDatabaseService.cs** → Split functionality
   - Core database operations → Move to `Sql2Csv.Core/Services/DatabaseAnalysisService.cs`
   - Web-specific file upload logic → Keep minimal wrapper in web project

3. **Performance Services** → Already appropriate for web project (metrics, monitoring)

### Models to Move to Core

1. **PersistedFileModels.cs** → Move to `Sql2Csv.Core/Models/`
   - Core business entities
   - Remove web-specific attributes

2. **ViewModels.cs** → Split functionality
   - Business DTOs → Move to Core
   - Web-specific ViewModels → Keep in web project

3. **Analysis ViewModels** → Move to Core as business DTOs

## Implementation Steps

### Phase 1: Move Core Business Models

1. Move `PersistedDatabaseFile` to Core/Models
2. Create interfaces for file operations
3. Update namespaces and dependencies

### Phase 2: Move Core Services

1. Create abstraction for file upload operations
2. Move core database analysis logic to Core
3. Create web-agnostic service interfaces
4. Update dependency injection

### Phase 3: Update Web Project

1. Create thin web service wrappers
2. Update controllers to use new service interfaces
3. Keep web-specific ViewModels in web project
4. Update using statements

### Phase 4: Testing and Validation

1. Run all tests to ensure functionality
2. Verify web application still works
3. Update any broken references

## Dependencies Considerations

### Core Library Constraints

- Cannot reference ASP.NET Core types (IFormFile, IConfiguration)
- Must use abstractions for web-specific operations
- Should remain web-agnostic

### Interface Abstractions Needed

```csharp
// For file upload operations
public interface IFileUploadInfo
{
    string FileName { get; }
    long Length { get; }
    Stream OpenReadStream();
}

// For configuration
public interface IFileStorageOptions
{
    string PersistedDirectory { get; }
}
```

## Expected Benefits

1. **Reduced Web Project Size**: Remove ~2000+ lines of business logic code
2. **Better Separation of Concerns**: Clear distinction between web and business logic
3. **Improved Reusability**: Core services can be used by console app and future projects
4. **Easier Testing**: Business logic testing doesn't require web test infrastructure
5. **Clean Architecture**: Follows dependency inversion principle

## Risk Mitigation

1. **Careful Interface Design**: Ensure abstractions don't leak web concerns
2. **Gradual Migration**: Move one service at a time to minimize disruption
3. **Comprehensive Testing**: Run full test suite after each major move
4. **Backward Compatibility**: Ensure existing functionality remains intact
