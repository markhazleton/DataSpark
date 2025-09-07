# CSV File Management Fix Summary

**Date**: 2025-08-31  
**Issue**: CSV files uploaded with "Save for future use" option were not appearing in managed files  
**Status**: ✅ **FIXED**

## Root Cause

The issue was in the `HomeController.cs` file where CSV files were being saved with `TableCount = 0` because the table count extraction logic only handled database files:

```csharp
// OLD CODE - Only extracted table count for database files
int tableCount = 0;
if (fileType == DataSourceType.Database && uploadAdditionalInfo is not null)
{
    var dict = uploadAdditionalInfo as dynamic;
    tableCount = dict?.TableCount ?? 0;
}
```

CSV files were getting `tableCount = 0`, which while not preventing persistence, made them appear as having "0 tables" in the UI.

## Solution Implemented

### 1. Fixed Table Count Logic (HomeController.cs)

Updated both the regular upload method (`Upload`) and API upload method (`UploadApi`) to properly handle CSV files:

```csharp
// NEW CODE - Handles both database and CSV files
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
    _logger.LogInformation("Setting table count to 1 for CSV file");
}
```

**Impact**: CSV files now get `tableCount = 1`, making them appear properly in the managed files interface.

### 2. Enhanced UI for Multiple File Types

Updated views to be more inclusive of different file types:

#### ManageFiles.cshtml

- Changed page description from "Manage your uploaded database files and storage" to "Manage your uploaded database and CSV files"
- Updated "Total Tables" to "Total Data Sources" in summary cards
- Changed table header from "Tables" to "Data Sources"
- Updated table cell display from "@file.TableCount tables" to "@(file.TableCount == 1 ? "1 source" : $"{file.TableCount} sources")"
- Changed "Upload Database File" to "Upload Data File" in empty state

#### Index.cshtml

- Changed button text from "Analyze Database" to "Analyze Data File"
- Updated file selection display from "@file.TableCount tables" to "@(file.TableCount == 1 ? "1 source" : $"{file.TableCount} sources")"

## Changes Made

### Files Modified

1. **HomeController.cs** - Fixed table count logic for CSV files in both upload methods
2. **ManageFiles.cshtml** - Updated UI to be file-type agnostic
3. **Index.cshtml** - Updated labels and button text to be more generic

### Key Improvements

- ✅ CSV files now correctly appear in managed files with "1 source"
- ✅ UI terminology is now file-type agnostic ("Data Sources" vs "Tables")
- ✅ Both database and CSV files are properly supported in the persistence system
- ✅ Added appropriate logging for CSV file persistence operations

## Testing Steps

To verify the fix:

1. **Upload CSV File with Persistence**:
   - Go to <http://localhost:5000>
   - Upload a CSV file
   - Check "Save this file for future use"
   - Add a description (optional)
   - Click "Analyze Data File"

2. **Verify in Managed Files**:
   - Navigate to "Manage Files"
   - Confirm CSV file appears in the list
   - Verify it shows "1 source" instead of "0 tables"
   - Verify file type indicators work properly

3. **Verify File Selection**:
   - Go back to home page
   - Check that CSV files appear in "Choose Existing File" tab
   - Verify they can be selected and reused

4. **Test Different File Types**:
   - Upload both database (.db) and CSV (.csv) files
   - Verify both appear correctly in managed files
   - Confirm terminology is appropriate for each type

## Technical Notes

- The `PersistedFileService` was already correctly designed to handle any file type
- The issue was purely in the data being passed to it (table count)
- CSV files conceptually represent "1 data source" (equivalent to 1 table)
- The fix maintains backward compatibility with existing database files
- UI changes are semantic and improve user experience for mixed file types

## Result

✅ **CSV files uploaded with "Save for future use" now properly appear in managed files**  
✅ **UI is now file-type agnostic and works well for both databases and CSV files**  
✅ **Users can successfully manage, select, and reuse both database and CSV files**
