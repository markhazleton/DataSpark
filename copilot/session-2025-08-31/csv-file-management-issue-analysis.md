# CSV File Management Issue Analysis

**Date**: 2025-08-31  
**Focus**: CSV file upload with save option not appearing in managed files  
**Files Modified**: HomeController.cs (will be fixed)

## Issue Summary

When uploading CSV files with the "Save for future use" option enabled, the files are being saved to the persisted file system but are not properly handled in the managed files interface. The root cause is that CSV files are being saved with `TableCount = 0`, and the UI is designed around database-centric concepts.

## Root Cause Analysis

### 1. Table Count Logic Issue

In `HomeController.cs`, both the regular upload (`Upload` action) and API upload (`UploadApi` action) have the same problematic logic:

```csharp
// For database files, get table count from additional info
int tableCount = 0;
if (fileType == DataSourceType.Database && uploadAdditionalInfo is not null)
{
    var dict = uploadAdditionalInfo as dynamic;
    tableCount = dict?.TableCount ?? 0;
}

await _persistedFileService.SavePersistedFileAsync(file, filePath, tableCount, description);
```

**Problem**: The condition `fileType == DataSourceType.Database` means that CSV files always get `tableCount = 0`, even though CSV files conceptually represent one data source (equivalent to one "table").

### 2. UI Database-Centric Design

The ManageFiles view and related UI components are designed around database concepts:

- Shows "Tables" column with `@file.TableCount tables`
- Displays "Total Tables" in summary
- Uses database icons and terminology

However, the `PersistedFileService` itself properly supports both file types - the issue is in the data being passed to it.

### 3. Additional Info Extraction

For CSV files, the `UnifiedWebDataService.SaveCsvFileAsync` method returns additional info including:

- `ColumnCount`
- `RowCount`
- `HasHeaders`
- `Delimiter`

But this information is not being used when saving CSV files to the persisted storage.

## Solution Strategy

### 1. Fix Table Count Logic

Update the table count extraction in both upload methods to handle CSV files properly:

```csharp
int tableCount = 0;
if (fileType == DataSourceType.Database && uploadAdditionalInfo is not null)
{
    var dict = uploadAdditionalInfo as dynamic;
    tableCount = dict?.TableCount ?? 0;
}
else if (fileType == DataSourceType.Csv)
{
    // CSV files represent one data source (conceptually one "table")
    tableCount = 1;
}
```

### 2. Enhance UI for Multiple File Types

Consider updating the ManageFiles view to be more generic:

- Change "Tables" to "Data Sources" or "Sources"
- Show file type indicators
- Display appropriate metadata for each file type

### 3. Store Additional CSV Metadata

Consider storing CSV-specific metadata (row count, column count) in the `PersistedDatabaseFile` model for better display and management.

## Impact

This fix will ensure that:

1. CSV files appear properly in the managed files list
2. The UI correctly shows CSV files as having "1 table" (data source)
3. Users can select and reuse uploaded CSV files
4. The persistence feature works consistently for both database and CSV files

## Testing Required

After implementing the fix:

1. Upload CSV file with "Save for future use" checked
2. Verify file appears in Manage Files
3. Verify file can be selected from existing files dropdown
4. Verify file statistics are displayed correctly
5. Test with various CSV file sizes and formats
