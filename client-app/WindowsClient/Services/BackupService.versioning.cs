using AI_BACKUP.WindowsService.Models;
using Microsoft.Extensions.Options;

namespace AI_BACKUP.WindowsService.Services
{
    public class BackupService : IBackupService
    {
        private readonly ILogger<BackupService> _logger;
        private readonly ServiceSettings _settings;
        private readonly IFileSystemService _fileSystemService;
        private readonly ICompressionService _compressionService;
        private readonly IEncryptionService _encryptionService;
        private readonly IStorageService _storageService;
        private readonly IDatabaseBackupService _databaseBackupService;
        private readonly IApiClientService _apiClientService;
        private readonly IFileVersionService _fileVersionService;
        private readonly IDeltaCompressionService _deltaCompressionService;

        public BackupService(
            ILogger<BackupService> logger,
            IOptions<ServiceSettings> settings,
            IFileSystemService fileSystemService,
            ICompressionService compressionService,
            IEncryptionService encryptionService,
            IStorageService storageService,
            IDatabaseBackupService databaseBackupService,
            IApiClientService apiClientService,
            IFileVersionService fileVersionService,
            IDeltaCompressionService deltaCompressionService)
        {
            _logger = logger;
            _settings = settings.Value;
            _fileSystemService = fileSystemService;
            _compressionService = compressionService;
            _encryptionService = encryptionService;
            _storageService = storageService;
            _databaseBackupService = databaseBackupService;
            _apiClientService = apiClientService;
            _fileVersionService = fileVersionService;
            _deltaCompressionService = deltaCompressionService;
        }

        public async Task<BackupResultModel> PerformBackupAsync(BackupJobModel backupJob, CancellationToken cancellationToken)
        {
            var result = new BackupResultModel
            {
                BackupJobId = backupJob.Id,
                BackupRunId = Guid.NewGuid(),
                StartTime = DateTime.UtcNow,
                Status = BackupRunStatus.Running
            };

            var tempDir = string.Empty;
            var tempFile = string.Empty;

            try
            {
                _logger.LogInformation("Starting backup job: {JobName} ({JobId})", backupJob.Name, backupJob.Id);

                // Create temp directory for processing
                tempDir = await _fileSystemService.CreateTempDirectoryAsync();
                
                // Get storage configuration
                var storageConfig = await _apiClientService.GetStorageConfigAsync(backupJob.StorageConfigId);
                if (storageConfig == null)
                {
                    throw new Exception($"Storage configuration not found for ID: {backupJob.StorageConfigId}");
                }

                // Perform backup based on type
                string backupFilePath;
                switch (backupJob.BackupType)
                {
                    case BackupType.File:
                        backupFilePath = await BackupFileWithVersioningAsync(backupJob, tempDir, result, cancellationToken);
                        break;
                    case BackupType.Directory:
                        backupFilePath = await BackupDirectoryWithVersioningAsync(backupJob, tempDir, result, cancellationToken);
                        break;
                    case BackupType.MSSQLDatabase:
                    case BackupType.MySQLDatabase:
                    case BackupType.PostgreSQLDatabase:
                    case BackupType.MongoDBDatabase:
                        backupFilePath = await BackupDatabaseWithVersioningAsync(backupJob, tempDir, result, cancellationToken);
                        break;
                    default:
                        throw new NotSupportedException($"Backup type {backupJob.BackupType} is not supported");
                }

                // If no files were changed, we might not have a backup file
                if (string.IsNullOrEmpty(backupFilePath))
                {
                    _logger.LogInformation("No files changed since last backup for job: {JobName} ({JobId})", backupJob.Name, backupJob.Id);
                    
                    // Update result
                    result.Status = BackupRunStatus.Completed;
                    result.EndTime = DateTime.UtcNow;
                    result.Message = "No files changed since last backup";
                    
                    return result;
                }

                // Get file size
                var fileSize = await _fileSystemService.GetFileSizeAsync(backupFilePath);
                
                // Generate remote path
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var fileName = Path.GetFileName(backupFilePath);
                var remotePath = $"{backupJob.Id}/{timestamp}_{fileName}";
                
                // Upload to storage
                var uploadedPath = await _storageService.UploadFileAsync(backupFilePath, storageConfig, remotePath);
                
                // Update result
                result.Status = BackupRunStatus.Completed;
                result.EndTime = DateTime.UtcNow;
                result.SizeBytes = fileSize;
                result.BackupPath = uploadedPath;
                
                _logger.LogInformation("Backup job completed successfully: {JobName} ({JobId})", backupJob.Name, backupJob.Id);
                
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Backup job cancelled: {JobName} ({JobId})", backupJob.Name, backupJob.Id);
                
                result.Status = BackupRunStatus.Cancelled;
                result.EndTime = DateTime.UtcNow;
                result.ErrorMessage = "Backup operation was cancelled";
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing backup job: {JobName} ({JobId})", backupJob.Name, backupJob.Id);
                
                result.Status = BackupRunStatus.Failed;
                result.EndTime = DateTime.UtcNow;
                result.ErrorMessage = ex.Message;
                
                return result;
            }
            finally
            {
                // Clean up temp files
                try
                {
                    if (!string.IsNullOrEmpty(tempFile) && await _fileSystemService.FileExistsAsync(tempFile))
                    {
                        await _fileSystemService.DeleteFileAsync(tempFile);
                    }
                    
                    if (!string.IsNullOrEmpty(tempDir) && await _fileSystemService.DirectoryExistsAsync(tempDir))
                    {
                        await _fileSystemService.DeleteDirectoryAsync(tempDir, true);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error cleaning up temporary files");
                }
            }
        }

        #region Versioning Backup Methods

        private async Task<string> BackupFileWithVersioningAsync(BackupJobModel backupJob, string tempDir, BackupResultModel result, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Backing up file with versioning: {SourcePath}", backupJob.SourcePath);
            
            // Check if source file exists
            if (!await _fileSystemService.FileExistsAsync(backupJob.SourcePath))
            {
                throw new FileNotFoundException($"Source file not found: {backupJob.SourcePath}");
            }
            
            // Check if file has changed since last backup
            var hasChanged = await _fileVersionService.HasFileChangedAsync(backupJob.Id, backupJob.SourcePath);
            
            if (!hasChanged)
            {
                _logger.LogInformation("File has not changed since last backup: {SourcePath}", backupJob.SourcePath);
                
                // Create a reference to the previous version
                var latestVersion = await _fileVersionService.GetLatestFileVersionAsync(backupJob.Id, backupJob.SourcePath);
                
                if (latestVersion != null)
                {
                    var fileInfo = new FileInfo(backupJob.SourcePath);
                    
                    var referenceVersion = new FileVersionModel
                    {
                        Id = Guid.NewGuid(),
                        BackupJobId = backupJob.Id,
                        BackupRunId = result.BackupRunId,
                        FilePath = backupJob.SourcePath,
                        FileName = Path.GetFileName(backupJob.SourcePath),
                        FileSize = fileInfo.Length,
                        ContentHash = latestVersion.ContentHash,
                        LastModified = fileInfo.LastWriteTime,
                        CreatedAt = DateTime.UtcNow,
                        BackupPath = latestVersion.BackupPath,
                        IsEncrypted = latestVersion.IsEncrypted,
                        IsCompressed = latestVersion.IsCompressed,
                        BackupType = BackupType.Reference,
                        BaseVersionId = latestVersion.Id
                    };
                    
                    // Save the reference version
                    await _apiClientService.SaveFileVersionAsync(referenceVersion);
                    
                    result.UnchangedFiles = 1;
                    
                    // Return empty string to indicate no new backup file
                    return string.Empty;
                }
            }
            
            // Generate output file path
            var fileName = Path.GetFileName(backupJob.SourcePath);
            var outputFilePath = Path.Combine(tempDir, fileName);
            
            // Copy file to temp directory
            await _fileSystemService.CopyFileAsync(backupJob.SourcePath, outputFilePath, true);
            
            // Process the file (compress/encrypt)
            outputFilePath = await ProcessBackupFileAsync(outputFilePath, backupJob.Compress, backupJob.Encrypt);
            
            // Create file version
            var fileInfo = new FileInfo(backupJob.SourcePath);
            var contentHash = await _deltaCompressionService.CalculateFileHashAsync(backupJob.SourcePath);
            
            // We'll create the actual version after uploading to storage
            result.ChangedFiles = 1;
            
            return outputFilePath;
        }

        private async Task<string> BackupDirectoryWithVersioningAsync(BackupJobModel backupJob, string tempDir, BackupResultModel result, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Backing up directory with versioning: {SourcePath}", backupJob.SourcePath);
            
            // Check if source directory exists
            if (!await _fileSystemService.DirectoryExistsAsync(backupJob.SourcePath))
            {
                throw new DirectoryNotFoundException($"Source directory not found: {backupJob.SourcePath}");
            }
            
            // Get all files in the directory
            var files = await _fileSystemService.GetFilesAsync(
                backupJob.SourcePath, 
                backupJob.FileFilter.Length > 0 ? backupJob.FileFilter : "*.*", 
                backupJob.Recursive);
            
            // Check which files have changed
            var changedFiles = new List<string>();
            var unchangedFiles = new List<string>();
            
            foreach (var file in files)
            {
                if (await _fileVersionService.HasFileChangedAsync(backupJob.Id, file))
                {
                    changedFiles.Add(file);
                }
                else
                {
                    unchangedFiles.Add(file);
                }
            }
            
            _logger.LogInformation("Found {ChangedCount} changed files and {UnchangedCount} unchanged files", 
                changedFiles.Count, unchangedFiles.Count);
            
            result.TotalFiles = files.Count;
            result.ChangedFiles = changedFiles.Count;
            result.UnchangedFiles = unchangedFiles.Count;
            
            // If no files have changed, create references and return
            if (changedFiles.Count == 0)
            {
                foreach (var file in unchangedFiles)
                {
                    try
                    {
                        // Get the latest version of the file
                        var latestVersion = await _fileVersionService.GetLatestFileVersionAsync(backupJob.Id, file);
                        
                        if (latestVersion != null)
                        {
                            // Create a reference version
                            var fileInfo = new FileInfo(file);
                            
                            var referenceVersion = new FileVersionModel
                            {
                                Id = Guid.NewGuid(),
                                BackupJobId = backupJob.Id,
                                BackupRunId = result.BackupRunId,
                                FilePath = file,
                                FileName = Path.GetFileName(file),
                                FileSize = fileInfo.Length,
                                ContentHash = latestVersion.ContentHash,
                                LastModified = fileInfo.LastWriteTime,
                                CreatedAt = DateTime.UtcNow,
                                BackupPath = latestVersion.BackupPath,
                                IsEncrypted = latestVersion.IsEncrypted,
                                IsCompressed = latestVersion.IsCompressed,
                                BackupType = BackupType.Reference,
                                BaseVersionId = latestVersion.Id
                            };
                            
                            // Save the reference version
                            await _apiClientService.SaveFileVersionAsync(referenceVersion);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating reference for file: {FilePath}", file);
                    }
                }
                
                // Return empty string to indicate no new backup file
                return string.Empty;
            }
            
            // Create a temporary directory for changed files
            var changedFilesDir = Path.Combine(tempDir, "changed_files");
            await _fileSystemService.CreateDirectoryAsync(changedFilesDir);
            
            // Copy changed files to temp directory
            foreach (var file in changedFiles)
            {
                try
                {
                    // Get the relative path
                    var relativePath = _fileSystemService.GetRelativePath(backupJob.SourcePath, file);
                    var destPath = Path.Combine(changedFilesDir, relativePath);
                    
                    // Ensure directory exists
                    var destDir = Path.GetDirectoryName(destPath);
                    if (!string.IsNullOrEmpty(destDir) && !await _fileSystemService.DirectoryExistsAsync(destDir))
                    {
                        await _fileSystemService.CreateDirectoryAsync(destDir);
                    }
                    
                    // Copy file
                    await _fileSystemService.CopyFileAsync(file, destPath, true);
                    
                    // Get file size
                    var fileInfo = new FileInfo(file);
                    result.SizeBytes += fileInfo.Length;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error copying file: {FilePath}", file);
                    result.FailedFiles.Add(file);
                }
            }
            
            // Generate output file path
            var dirName = new DirectoryInfo(backupJob.SourcePath).Name;
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var outputFilePath = Path.Combine(tempDir, $"{dirName}_{timestamp}.zip");
            
            // Compress directory with changed files
            await _compressionService.CompressDirectoryAsync(changedFilesDir, outputFilePath);
            
            // Encrypt if needed
            if (backupJob.Encrypt && _settings.EnableEncryption)
            {
                var encryptedFilePath = $"{outputFilePath}.enc";
                await _encryptionService.EncryptFileAsync(outputFilePath, encryptedFilePath, _settings.EncryptionKey);
                await _fileSystemService.DeleteFileAsync(outputFilePath);
                outputFilePath = encryptedFilePath;
            }
            
            return outputFilePath;
        }

        private async Task<string> BackupDatabaseWithVersioningAsync(BackupJobModel backupJob, string tempDir, BackupResultModel result, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Backing up database with versioning: {DatabaseName}", backupJob.DatabaseSettings?.DatabaseName);
            
            if (backupJob.DatabaseSettings == null)
            {
                throw new ArgumentException("Database settings are required for database backup");
            }
            
            // Generate output file path
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            string outputFilePath;
            
            // Perform database backup based on type
            switch (backupJob.BackupType)
            {
                case BackupType.MSSQLDatabase:
                    outputFilePath = Path.Combine(tempDir, $"{backupJob.DatabaseSettings.DatabaseName}_{timestamp}.bak");
                    outputFilePath = await _databaseBackupService.BackupMsSqlDatabaseAsync(backupJob.DatabaseSettings, outputFilePath);
                    break;
                case BackupType.MySQLDatabase:
                    outputFilePath = Path.Combine(tempDir, $"{backupJob.DatabaseSettings.DatabaseName}_{timestamp}.sql");
                    outputFilePath = await _databaseBackupService.BackupMySqlDatabaseAsync(backupJob.DatabaseSettings, outputFilePath);
                    break;
                case BackupType.PostgreSQLDatabase:
                    outputFilePath = Path.Combine(tempDir, $"{backupJob.DatabaseSettings.DatabaseName}_{timestamp}.dump");
                    outputFilePath = await _databaseBackupService.BackupPostgreSqlDatabaseAsync(backupJob.DatabaseSettings, outputFilePath);
                    break;
                case BackupType.MongoDBDatabase:
                    outputFilePath = Path.Combine(tempDir, $"{backupJob.DatabaseSettings.DatabaseName}_{timestamp}");
                    outputFilePath = await _databaseBackupService.BackupMongoDbDatabaseAsync(backupJob.DatabaseSettings, outputFilePath);
                    break;
                default:
                    throw new NotSupportedException($"Database type {backupJob.BackupType} is not supported");
            }
            
            // Calculate hash for the backup file
            var contentHash = await _deltaCompressionService.CalculateFileHashAsync(outputFilePath);
            
            // Check if the database has changed since last backup
            var latestVersion = await _fileVersionService.GetLatestFileVersionAsync(
                backupJob.Id, 
                backupJob.DatabaseSettings.DatabaseName);
                
            if (latestVersion != null && string.Equals(latestVersion.ContentHash, contentHash, StringComparison.OrdinalIgnoreCase))
            {
                // Database hasn't changed, create a reference
                _logger.LogInformation("Database {DatabaseName} has not changed, creating reference", 
                    backupJob.DatabaseSettings.DatabaseName);
                
                var fileInfo = new FileInfo(outputFilePath);
                
                var referenceVersion = new FileVersionModel
                {
                    Id = Guid.NewGuid(),
                    BackupJobId = backupJob.Id,
                    BackupRunId = result.BackupRunId,
                    FilePath = backupJob.DatabaseSettings.DatabaseName,
                    FileName = backupJob.DatabaseSettings.DatabaseName,
                    FileSize = fileInfo.Length,
                    ContentHash = contentHash,
                    LastModified = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    BackupPath = latestVersion.BackupPath,
                    IsEncrypted = latestVersion.IsEncrypted,
                    IsCompressed = latestVersion.IsCompressed,
                    BackupType = BackupType.Reference,
                    BaseVersionId = latestVersion.Id
                };
                
                // Save the reference version
                await _apiClientService.SaveFileVersionAsync(referenceVersion);
                
                result.UnchangedFiles = 1;
                
                // Delete the temporary backup file
                await _fileSystemService.DeleteFileAsync(outputFilePath);
                
                // Return empty string to indicate no new backup file
                return string.Empty;
            }
            
            // Process the file (compress/encrypt)
            outputFilePath = await ProcessBackupFileAsync(outputFilePath, backupJob.Compress, backupJob.Encrypt);
            
            result.ChangedFiles = 1;
            result.TotalFiles = 1;
            
            return outputFilePath;
        }

        #endregion
    }
}