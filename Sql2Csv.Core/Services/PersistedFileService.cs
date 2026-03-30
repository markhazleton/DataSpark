using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sql2Csv.Core.Interfaces;
using Sql2Csv.Core.Models;

namespace Sql2Csv.Core.Services;

/// <summary>
/// Service for managing persisted database files
/// </summary>
public class PersistedFileService : IPersistedFileService
{
    private readonly ILogger<PersistedFileService> _logger;
    private readonly IFileStorageOptions _storageOptions;
    private readonly string _persistedDirectory;
    private readonly string _metadataFilePath;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public PersistedFileService(
        ILogger<PersistedFileService> logger,
        IFileStorageOptions storageOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _storageOptions = storageOptions ?? throw new ArgumentNullException(nameof(storageOptions));

        _persistedDirectory = storageOptions.PersistedDirectory;
        _metadataFilePath = Path.Combine(_persistedDirectory, "metadata.json");

        // Ensure directory exists
        Directory.CreateDirectory(_persistedDirectory);
    }

    public async Task<List<PersistedDatabaseFile>> GetPersistedFilesAsync()
    {
        await _fileLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!File.Exists(_metadataFilePath))
            {
                return [];
            }

            var json = await File.ReadAllTextAsync(_metadataFilePath).ConfigureAwait(false);
            var files = JsonSerializer.Deserialize<List<PersistedDatabaseFile>>(json, _jsonOptions) ?? [];

            // Filter out files that no longer exist on disk
            var existingFiles = new List<PersistedDatabaseFile>();
            foreach (var file in files)
            {
                if (File.Exists(file.StoredFilePath))
                {
                    existingFiles.Add(file);
                }
                else
                {
                    _logger.LogWarning("Persisted file {FilePath} no longer exists, removing from metadata", file.StoredFilePath);
                }
            }

            // Update metadata if files were removed
            if (existingFiles.Count != files.Count)
            {
                await SaveMetadataAsync(existingFiles).ConfigureAwait(false);
            }

            return existingFiles.OrderByDescending(f => f.LastAccessedAt).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading persisted files metadata");
            return [];
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<PersistedDatabaseFile?> GetPersistedFileAsync(string fileId)
    {
        // This method can reuse GetPersistedFilesAsync since it doesn't hold a lock itself
        var files = await GetPersistedFilesAsync().ConfigureAwait(false);
        return files.FirstOrDefault(f => f.Id == fileId);
    }

    public async Task<List<PersistedDatabaseFile>> GetAvailableFilesAsync()
    {
        return await GetPersistedFilesAsync().ConfigureAwait(false);
    }

    public async Task<PersistedDatabaseFile> SavePersistedFileAsync(
        IUploadedFileInfo file,
        string tempFilePath,
        int tableCount,
        string? description = null)
    {
        await _fileLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var fileId = Guid.NewGuid().ToString();
            var fileExtension = Path.GetExtension(file.FileName);
            var persistedFileName = $"{fileId}{fileExtension}";
            var persistedFilePath = Path.Combine(_persistedDirectory, persistedFileName);

            // Add a small delay to ensure any database connections are fully released
            await Task.Delay(100).ConfigureAwait(false);

            // Copy temp file to persisted location with retry logic
            await CopyFileWithRetryAsync(tempFilePath, persistedFilePath).ConfigureAwait(false);

            var persistedFile = new PersistedDatabaseFile
            {
                Id = fileId,
                OriginalFileName = file.FileName,
                StoredFilePath = persistedFilePath,
                UploadedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow,
                FileSizeBytes = file.Length,
                TableCount = tableCount,
                Description = description
            };

            // Read existing files directly without calling GetPersistedFilesAsync to avoid deadlock
            var files = new List<PersistedDatabaseFile>();
            if (File.Exists(_metadataFilePath))
            {
                var json = await File.ReadAllTextAsync(_metadataFilePath).ConfigureAwait(false);
                files = JsonSerializer.Deserialize<List<PersistedDatabaseFile>>(json, _jsonOptions) ?? [];
            }

            // Add to metadata
            files.Add(persistedFile);
            await SaveMetadataAsync(files).ConfigureAwait(false);

            _logger.LogInformation("Persisted database file {FileName} with ID {FileId}", file.FileName, fileId);
            return persistedFile;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<bool> DeletePersistedFileAsync(string fileId)
    {
        await _fileLock.WaitAsync().ConfigureAwait(false);
        try
        {
            // Read files directly to avoid deadlock
            var files = new List<PersistedDatabaseFile>();
            if (File.Exists(_metadataFilePath))
            {
                var json = await File.ReadAllTextAsync(_metadataFilePath).ConfigureAwait(false);
                files = JsonSerializer.Deserialize<List<PersistedDatabaseFile>>(json, _jsonOptions) ?? [];
            }

            var fileToDelete = files.FirstOrDefault(f => f.Id == fileId);

            if (fileToDelete == null)
            {
                return false;
            }

            // Delete physical file
            if (File.Exists(fileToDelete.StoredFilePath))
            {
                File.Delete(fileToDelete.StoredFilePath);
            }

            // Remove from metadata
            files.Remove(fileToDelete);
            await SaveMetadataAsync(files).ConfigureAwait(false);

            _logger.LogInformation("Deleted persisted file {FileId}", fileId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting persisted file {FileId}", fileId);
            return false;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<bool> UpdateFileDescriptionAsync(string fileId, string? description)
    {
        await _fileLock.WaitAsync().ConfigureAwait(false);
        try
        {
            // Read files directly to avoid deadlock
            var files = new List<PersistedDatabaseFile>();
            if (File.Exists(_metadataFilePath))
            {
                var json = await File.ReadAllTextAsync(_metadataFilePath).ConfigureAwait(false);
                files = JsonSerializer.Deserialize<List<PersistedDatabaseFile>>(json, _jsonOptions) ?? [];
            }

            var fileToUpdate = files.FirstOrDefault(f => f.Id == fileId);

            if (fileToUpdate == null)
            {
                return false;
            }

            // Create updated file record
            var updatedFile = fileToUpdate with { Description = description };
            var index = files.IndexOf(fileToUpdate);
            files[index] = updatedFile;

            await SaveMetadataAsync(files).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating file description for {FileId}", fileId);
            return false;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task UpdateLastAccessedAsync(string fileId)
    {
        await _fileLock.WaitAsync().ConfigureAwait(false);
        try
        {
            // Read files directly to avoid deadlock
            var files = new List<PersistedDatabaseFile>();
            if (File.Exists(_metadataFilePath))
            {
                var json = await File.ReadAllTextAsync(_metadataFilePath).ConfigureAwait(false);
                files = JsonSerializer.Deserialize<List<PersistedDatabaseFile>>(json, _jsonOptions) ?? [];
            }

            var fileToUpdate = files.FirstOrDefault(f => f.Id == fileId);

            if (fileToUpdate == null)
            {
                return;
            }

            // Create updated file record
            var updatedFile = fileToUpdate with { LastAccessedAt = DateTime.UtcNow };
            var index = files.IndexOf(fileToUpdate);
            files[index] = updatedFile;

            await SaveMetadataAsync(files).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last accessed time for {FileId}", fileId);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task CleanupOldFilesAsync(TimeSpan maxAge)
    {
        await _fileLock.WaitAsync().ConfigureAwait(false);
        try
        {
            // Read files directly to avoid deadlock
            var files = new List<PersistedDatabaseFile>();
            if (File.Exists(_metadataFilePath))
            {
                var json = await File.ReadAllTextAsync(_metadataFilePath).ConfigureAwait(false);
                files = JsonSerializer.Deserialize<List<PersistedDatabaseFile>>(json, _jsonOptions) ?? [];
            }

            var cutoffDate = DateTime.UtcNow - maxAge;
            var filesToDelete = files.Where(f => f.LastAccessedAt < cutoffDate).ToList();

            // Delete physical files and remove from list
            foreach (var file in filesToDelete)
            {
                // Delete physical file
                if (File.Exists(file.StoredFilePath))
                {
                    try
                    {
                        File.Delete(file.StoredFilePath);
                        _logger.LogInformation("Deleted old persisted file {FileId}", file.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deleting physical file for {FileId}", file.Id);
                    }
                }

                // Remove from files list
                files.Remove(file);
            }

            // Save updated metadata if files were deleted
            if (filesToDelete.Any())
            {
                await SaveMetadataAsync(files).ConfigureAwait(false);
                _logger.LogInformation("Cleaned up {Count} old persisted files", filesToDelete.Count);
            }
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<long> GetTotalStorageSizeAsync()
    {
        var files = await GetPersistedFilesAsync().ConfigureAwait(false);
        return files.Sum(f => f.FileSizeBytes);
    }

    private async Task SaveMetadataAsync(List<PersistedDatabaseFile> files)
    {
        var json = JsonSerializer.Serialize(files, _jsonOptions);
        await File.WriteAllTextAsync(_metadataFilePath, json).ConfigureAwait(false);
    }

    private async Task CopyFileWithRetryAsync(string sourcePath, string destinationPath, int maxRetries = 3)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                File.Copy(sourcePath, destinationPath, overwrite: true);
                return; // Success
            }
            catch (IOException ex) when (i < maxRetries - 1)
            {
                _logger.LogWarning(ex, "File copy attempt {Attempt} failed, retrying in {Delay}ms", i + 1, 250 * (i + 1));
                await Task.Delay(250 * (i + 1)).ConfigureAwait(false); // Progressive delay: 250ms, 500ms, 750ms
            }
        }

        // Final attempt without catch - let exception propagate
        File.Copy(sourcePath, destinationPath, overwrite: true);
    }
}
