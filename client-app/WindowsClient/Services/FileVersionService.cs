using AI_BACKUP.WindowsService.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AI_BACKUP.WindowsService.Services
{
    /// <summary>
    /// Service for managing file versions and metadata
    /// </summary>
    public class FileVersionService : IFileVersionService
    {
        private readonly ILogger<FileVersionService> _logger;
        private readonly IFileSystemService _fileSystemService;
        private readonly IDeltaCompressionService _deltaCompressionService;
        private readonly IApiClientService _apiClientService;
        private readonly string _metadataDirectory;

        public FileVersionService(
            ILogger<FileVersionService> logger,
            IFileSystemService fileSystemService,
            IDeltaCompressionService deltaCompressionService,
            IApiClientService apiClientService)
        {
            _logger = logger;
            _fileSystemService = fileSystemService;
            _deltaCompressionService = deltaCompressionService;
            _apiClientService = apiClientService;
            
            // Create a directory for storing metadata locally
            _metadataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AI_BACKUP",
                "Metadata");
                
            if (!Directory.Exists(_metadataDirectory))
            {
                Directory.CreateDirectory(_metadataDirectory);
            }
        }

        /// <summary>
        /// Gets the latest version of a file for a specific backup job
        /// </summary>
        /// <param name="backupJobId">ID of the backup job</param>
        /// <param name="filePath">Path of the file</param>
        /// <returns>The latest file version, or null if not found</returns>
        public async Task<FileVersionModel> GetLatestFileVersionAsync(Guid backupJobId, string filePath)
        {
            try
            {
                _logger.LogDebug("Getting latest version for file {FilePath} in job {BackupJobId}", filePath, backupJobId);
                
                // First try to get from API
                var version = await _apiClientService.GetLatestFileVersionAsync(backupJobId, filePath);
                
                // If not found in API, try to get from local metadata
                if (version == null)
                {
                    version = await GetLatestFileVersionFromLocalAsync(backupJobId, filePath);
                }
                
                return version;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting latest version for file {FilePath} in job {BackupJobId}", filePath, backupJobId);
                return null;
            }
        }

        /// <summary>
        /// Gets all versions of a file for a specific backup job
        /// </summary>
        /// <param name="backupJobId">ID of the backup job</param>
        /// <param name="filePath">Path of the file</param>
        /// <returns>List of file versions</returns>
        public async Task<List<FileVersionModel>> GetFileVersionsAsync(Guid backupJobId, string filePath)
        {
            try
            {
                _logger.LogDebug("Getting all versions for file {FilePath} in job {BackupJobId}", filePath, backupJobId);
                
                // First try to get from API
                var versions = await _apiClientService.GetFileVersionsAsync(backupJobId, filePath);
                
                // If not found in API, try to get from local metadata
                if (versions == null || !versions.Any())
                {
                    versions = await GetFileVersionsFromLocalAsync(backupJobId, filePath);
                }
                
                return versions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting versions for file {FilePath} in job {BackupJobId}", filePath, backupJobId);
                return new List<FileVersionModel>();
            }
        }

        /// <summary>
        /// Creates a new file version
        /// </summary>
        /// <param name="backupJobId">ID of the backup job</param>
        /// <param name="backupRunId">ID of the backup run</param>
        /// <param name="filePath">Path of the file</param>
        /// <param name="backupPath">Path where the file is backed up</param>
        /// <param name="isEncrypted">Whether the file is encrypted</param>
        /// <param name="isCompressed">Whether the file is compressed</param>
        /// <returns>The created file version</returns>
        public async Task<FileVersionModel> CreateFileVersionAsync(
            Guid backupJobId,
            Guid backupRunId,
            string filePath,
            string backupPath,
            bool isEncrypted,
            bool isCompressed)
        {
            try
            {
                _logger.LogInformation("Creating new version for file {FilePath} in job {BackupJobId}", filePath, backupJobId);
                
                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }
                
                // Calculate hash
                var contentHash = await _deltaCompressionService.CalculateFileHashAsync(filePath);
                
                // Get latest version to check for changes
                var latestVersion = await GetLatestFileVersionAsync(backupJobId, filePath);
                
                // Create new version
                var version = new FileVersionModel
                {
                    Id = Guid.NewGuid(),
                    BackupJobId = backupJobId,
                    BackupRunId = backupRunId,
                    FilePath = filePath,
                    FileName = Path.GetFileName(filePath),
                    FileSize = fileInfo.Length,
                    ContentHash = contentHash,
                    LastModified = fileInfo.LastWriteTime,
                    CreatedAt = DateTime.UtcNow,
                    BackupPath = backupPath,
                    IsEncrypted = isEncrypted,
                    IsCompressed = isCompressed,
                    BackupType = BackupType.Full
                };
                
                // If there's a previous version with the same hash, create a reference
                if (latestVersion != null && string.Equals(latestVersion.ContentHash, contentHash, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("File {FilePath} has not changed, creating reference", filePath);
                    version.BackupType = BackupType.Reference;
                    version.BaseVersionId = latestVersion.Id;
                    version.BackupPath = latestVersion.BackupPath;
                }
                
                // Save to API
                await _apiClientService.SaveFileVersionAsync(version);
                
                // Save locally as well
                await SaveFileVersionLocallyAsync(version);
                
                return version;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating version for file {FilePath} in job {BackupJobId}", filePath, backupJobId);
                throw;
            }
        }

        /// <summary>
        /// Creates a delta version of a file
        /// </summary>
        /// <param name="backupJobId">ID of the backup job</param>
        /// <param name="backupRunId">ID of the backup run</param>
        /// <param name="filePath">Path of the file</param>
        /// <param name="baseVersion">Base version to create delta from</param>
        /// <param name="deltaPath">Path where the delta file will be saved</param>
        /// <param name="isEncrypted">Whether the delta is encrypted</param>
        /// <param name="isCompressed">Whether the delta is compressed</param>
        /// <returns>The created delta version</returns>
        public async Task<FileVersionModel> CreateDeltaVersionAsync(
            Guid backupJobId,
            Guid backupRunId,
            string filePath,
            FileVersionModel baseVersion,
            string deltaPath,
            bool isEncrypted,
            bool isCompressed)
        {
            try
            {
                _logger.LogInformation("Creating delta version for file {FilePath} in job {BackupJobId}", filePath, backupJobId);
                
                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }
                
                if (baseVersion == null)
                {
                    throw new ArgumentNullException(nameof(baseVersion), "Base version cannot be null");
                }
                
                // Calculate hash
                var contentHash = await _deltaCompressionService.CalculateFileHashAsync(filePath);
                
                // If the hash is the same, create a reference instead
                if (string.Equals(baseVersion.ContentHash, contentHash, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("File {FilePath} has not changed, creating reference", filePath);
                    
                    var referenceVersion = new FileVersionModel
                    {
                        Id = Guid.NewGuid(),
                        BackupJobId = backupJobId,
                        BackupRunId = backupRunId,
                        FilePath = filePath,
                        FileName = Path.GetFileName(filePath),
                        FileSize = fileInfo.Length,
                        ContentHash = contentHash,
                        LastModified = fileInfo.LastWriteTime,
                        CreatedAt = DateTime.UtcNow,
                        BackupPath = baseVersion.BackupPath,
                        IsEncrypted = baseVersion.IsEncrypted,
                        IsCompressed = baseVersion.IsCompressed,
                        BackupType = BackupType.Reference,
                        BaseVersionId = baseVersion.Id
                    };
                    
                    // Save to API
                    await _apiClientService.SaveFileVersionAsync(referenceVersion);
                    
                    // Save locally as well
                    await SaveFileVersionLocallyAsync(referenceVersion);
                    
                    return referenceVersion;
                }
                
                // Create temporary file for base version
                var tempBaseFile = Path.Combine(
                    Path.GetTempPath(),
                    $"AI_BACKUP_{Guid.NewGuid()}_{Path.GetFileName(filePath)}");
                
                try
                {
                    // Download base version
                    var baseFilePath = await _apiClientService.DownloadFileVersionAsync(baseVersion.Id, tempBaseFile);
                    
                    if (string.IsNullOrEmpty(baseFilePath) || !File.Exists(baseFilePath))
                    {
                        throw new FileNotFoundException($"Failed to download base version: {baseVersion.Id}");
                    }
                    
                    // Create delta
                    var success = await _deltaCompressionService.CreateDeltaAsync(baseFilePath, filePath, deltaPath);
                    
                    if (!success)
                    {
                        throw new Exception($"Failed to create delta for file: {filePath}");
                    }
                    
                    // Create delta version
                    var deltaVersion = new FileVersionModel
                    {
                        Id = Guid.NewGuid(),
                        BackupJobId = backupJobId,
                        BackupRunId = backupRunId,
                        FilePath = filePath,
                        FileName = Path.GetFileName(filePath),
                        FileSize = fileInfo.Length,
                        ContentHash = contentHash,
                        LastModified = fileInfo.LastWriteTime,
                        CreatedAt = DateTime.UtcNow,
                        BackupPath = deltaPath,
                        IsEncrypted = isEncrypted,
                        IsCompressed = isCompressed,
                        BackupType = BackupType.Delta,
                        BaseVersionId = baseVersion.Id
                    };
                    
                    // Save to API
                    await _apiClientService.SaveFileVersionAsync(deltaVersion);
                    
                    // Save locally as well
                    await SaveFileVersionLocallyAsync(deltaVersion);
                    
                    return deltaVersion;
                }
                finally
                {
                    // Clean up temporary file
                    if (File.Exists(tempBaseFile))
                    {
                        File.Delete(tempBaseFile);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating delta version for file {FilePath} in job {BackupJobId}", filePath, backupJobId);
                throw;
            }
        }

        /// <summary>
        /// Determines if a file has changed since its last backup
        /// </summary>
        /// <param name="backupJobId">ID of the backup job</param>
        /// <param name="filePath">Path of the file</param>
        /// <returns>True if the file has changed, false otherwise</returns>
        public async Task<bool> HasFileChangedAsync(Guid backupJobId, string filePath)
        {
            try
            {
                _logger.LogDebug("Checking if file {FilePath} has changed in job {BackupJobId}", filePath, backupJobId);
                
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("File {FilePath} does not exist", filePath);
                    return true; // Consider as changed if file doesn't exist
                }
                
                // Get latest version
                var latestVersion = await GetLatestFileVersionAsync(backupJobId, filePath);
                
                if (latestVersion == null)
                {
                    _logger.LogInformation("No previous version found for file {FilePath}, considering as changed", filePath);
                    return true; // No previous version, consider as changed
                }
                
                // Check if file has changed based on hash
                var hasChanged = await _deltaCompressionService.HasFileChangedAsync(filePath, latestVersion.ContentHash);
                
                _logger.LogDebug("File {FilePath} has changed: {HasChanged}", filePath, hasChanged);
                return hasChanged;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if file {FilePath} has changed in job {BackupJobId}", filePath, backupJobId);
                return true; // Consider as changed if there's an error
            }
        }

        #region Local Metadata Storage

        private async Task<FileVersionModel> GetLatestFileVersionFromLocalAsync(Guid backupJobId, string filePath)
        {
            try
            {
                var versions = await GetFileVersionsFromLocalAsync(backupJobId, filePath);
                return versions.OrderByDescending(v => v.CreatedAt).FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting latest version from local for file {FilePath} in job {BackupJobId}", filePath, backupJobId);
                return null;
            }
        }

        private async Task<List<FileVersionModel>> GetFileVersionsFromLocalAsync(Guid backupJobId, string filePath)
        {
            try
            {
                var normalizedPath = NormalizeFilePath(filePath);
                var metadataFile = GetMetadataFilePath(backupJobId, normalizedPath);
                
                if (!File.Exists(metadataFile))
                {
                    return new List<FileVersionModel>();
                }
                
                var json = await File.ReadAllTextAsync(metadataFile);
                return JsonConvert.DeserializeObject<List<FileVersionModel>>(json) ?? new List<FileVersionModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting versions from local for file {FilePath} in job {BackupJobId}", filePath, backupJobId);
                return new List<FileVersionModel>();
            }
        }

        private async Task SaveFileVersionLocallyAsync(FileVersionModel version)
        {
            try
            {
                var normalizedPath = NormalizeFilePath(version.FilePath);
                var metadataFile = GetMetadataFilePath(version.BackupJobId, normalizedPath);
                
                // Ensure directory exists
                var directory = Path.GetDirectoryName(metadataFile);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Get existing versions
                var versions = await GetFileVersionsFromLocalAsync(version.BackupJobId, version.FilePath);
                
                // Add new version
                versions.Add(version);
                
                // Save to file
                var json = JsonConvert.SerializeObject(versions, Formatting.Indented);
                await File.WriteAllTextAsync(metadataFile, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving version locally for file {FilePath} in job {BackupJobId}", version.FilePath, version.BackupJobId);
            }
        }

        private string GetMetadataFilePath(Guid backupJobId, string normalizedFilePath)
        {
            // Create a hash of the file path to avoid issues with special characters
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var pathBytes = System.Text.Encoding.UTF8.GetBytes(normalizedFilePath);
                var hashBytes = md5.ComputeHash(pathBytes);
                var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                
                return Path.Combine(_metadataDirectory, backupJobId.ToString(), $"{hashString}.json");
            }
        }

        private string NormalizeFilePath(string filePath)
        {
            // Normalize path separators and make lowercase for consistency
            return filePath.Replace('\\', '/').ToLowerInvariant();
        }

        #endregion
    }

    /// <summary>
    /// Interface for file version service
    /// </summary>
    public interface IFileVersionService
    {
        Task<FileVersionModel> GetLatestFileVersionAsync(Guid backupJobId, string filePath);
        Task<List<FileVersionModel>> GetFileVersionsAsync(Guid backupJobId, string filePath);
        Task<FileVersionModel> CreateFileVersionAsync(Guid backupJobId, Guid backupRunId, string filePath, string backupPath, bool isEncrypted, bool isCompressed);
        Task<FileVersionModel> CreateDeltaVersionAsync(Guid backupJobId, Guid backupRunId, string filePath, FileVersionModel baseVersion, string deltaPath, bool isEncrypted, bool isCompressed);
        Task<bool> HasFileChangedAsync(Guid backupJobId, string filePath);
    }
}