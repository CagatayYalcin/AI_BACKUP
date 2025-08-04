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

        public BackupService(
            ILogger<BackupService> logger,
            IOptions<ServiceSettings> settings,
            IFileSystemService fileSystemService,
            ICompressionService compressionService,
            IEncryptionService encryptionService,
            IStorageService storageService,
            IDatabaseBackupService databaseBackupService,
            IApiClientService apiClientService)
        {
            _logger = logger;
            _settings = settings.Value;
            _fileSystemService = fileSystemService;
            _compressionService = compressionService;
            _encryptionService = encryptionService;
            _storageService = storageService;
            _databaseBackupService = databaseBackupService;
            _apiClientService = apiClientService;
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
                        backupFilePath = await BackupFileAsync(backupJob, tempDir, cancellationToken);
                        break;
                    case BackupType.Directory:
                        backupFilePath = await BackupDirectoryAsync(backupJob, tempDir, cancellationToken);
                        break;
                    case BackupType.MSSQLDatabase:
                        backupFilePath = await BackupMsSqlDatabaseAsync(backupJob, tempDir, cancellationToken);
                        break;
                    case BackupType.MySQLDatabase:
                        backupFilePath = await BackupMySqlDatabaseAsync(backupJob, tempDir, cancellationToken);
                        break;
                    case BackupType.PostgreSQLDatabase:
                        backupFilePath = await BackupPostgreSqlDatabaseAsync(backupJob, tempDir, cancellationToken);
                        break;
                    case BackupType.MongoDBDatabase:
                        backupFilePath = await BackupMongoDbDatabaseAsync(backupJob, tempDir, cancellationToken);
                        break;
                    default:
                        throw new NotSupportedException($"Backup type {backupJob.BackupType} is not supported");
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

        public async Task<bool> VerifyBackupAsync(BackupResultModel backupResult)
        {
            try
            {
                _logger.LogInformation("Verifying backup: {BackupRunId}", backupResult.BackupRunId);
                
                // TODO: Implement backup verification
                // This could involve:
                // 1. Downloading the backup file
                // 2. Checking file integrity
                // 3. Verifying contents if possible
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying backup: {BackupRunId}", backupResult.BackupRunId);
                return false;
            }
        }

        public async Task<bool> RestoreBackupAsync(Guid backupRunId, string destinationPath)
        {
            try
            {
                _logger.LogInformation("Restoring backup: {BackupRunId} to {DestinationPath}", backupRunId, destinationPath);
                
                // TODO: Implement backup restoration
                // This would involve:
                // 1. Getting backup details from API
                // 2. Downloading the backup file
                // 3. Decompressing/decrypting if needed
                // 4. Restoring to the destination path
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring backup: {BackupRunId}", backupRunId);
                return false;
            }
        }

        #region Private Backup Methods

        private async Task<string> BackupFileAsync(BackupJobModel backupJob, string tempDir, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Backing up file: {SourcePath}", backupJob.SourcePath);
            
            // Check if source file exists
            if (!await _fileSystemService.FileExistsAsync(backupJob.SourcePath))
            {
                throw new FileNotFoundException($"Source file not found: {backupJob.SourcePath}");
            }
            
            // Generate output file path
            var fileName = Path.GetFileName(backupJob.SourcePath);
            var outputFilePath = Path.Combine(tempDir, fileName);
            
            // Copy file to temp directory
            await _fileSystemService.CopyFileAsync(backupJob.SourcePath, outputFilePath, true);
            
            // Process the file (compress/encrypt)
            outputFilePath = await ProcessBackupFileAsync(outputFilePath, backupJob.Compress, backupJob.Encrypt);
            
            return outputFilePath;
        }

        private async Task<string> BackupDirectoryAsync(BackupJobModel backupJob, string tempDir, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Backing up directory: {SourcePath}", backupJob.SourcePath);
            
            // Check if source directory exists
            if (!await _fileSystemService.DirectoryExistsAsync(backupJob.SourcePath))
            {
                throw new DirectoryNotFoundException($"Source directory not found: {backupJob.SourcePath}");
            }
            
            // Generate output file path
            var dirName = new DirectoryInfo(backupJob.SourcePath).Name;
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var outputFilePath = Path.Combine(tempDir, $"{dirName}_{timestamp}.zip");
            
            // Compress directory
            await _compressionService.CompressDirectoryAsync(
                backupJob.SourcePath, 
                outputFilePath, 
                backupJob.FileFilter.Length > 0 ? backupJob.FileFilter : "*.*", 
                backupJob.Recursive);
            
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

        private async Task<string> BackupMsSqlDatabaseAsync(BackupJobModel backupJob, string tempDir, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Backing up MSSQL database: {DatabaseName}", backupJob.DatabaseSettings?.DatabaseName);
            
            if (backupJob.DatabaseSettings == null)
            {
                throw new ArgumentException("Database settings are required for database backup");
            }
            
            // Generate output file path
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var outputFilePath = Path.Combine(tempDir, $"{backupJob.DatabaseSettings.DatabaseName}_{timestamp}.bak");
            
            // Perform database backup
            outputFilePath = await _databaseBackupService.BackupMsSqlDatabaseAsync(backupJob.DatabaseSettings, outputFilePath);
            
            // Process the file (compress/encrypt)
            outputFilePath = await ProcessBackupFileAsync(outputFilePath, backupJob.Compress, backupJob.Encrypt);
            
            return outputFilePath;
        }

        private async Task<string> BackupMySqlDatabaseAsync(BackupJobModel backupJob, string tempDir, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Backing up MySQL database: {DatabaseName}", backupJob.DatabaseSettings?.DatabaseName);
            
            if (backupJob.DatabaseSettings == null)
            {
                throw new ArgumentException("Database settings are required for database backup");
            }
            
            // Generate output file path
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var outputFilePath = Path.Combine(tempDir, $"{backupJob.DatabaseSettings.DatabaseName}_{timestamp}.sql");
            
            // Perform database backup
            outputFilePath = await _databaseBackupService.BackupMySqlDatabaseAsync(backupJob.DatabaseSettings, outputFilePath);
            
            // Process the file (compress/encrypt)
            outputFilePath = await ProcessBackupFileAsync(outputFilePath, backupJob.Compress, backupJob.Encrypt);
            
            return outputFilePath;
        }

        private async Task<string> BackupPostgreSqlDatabaseAsync(BackupJobModel backupJob, string tempDir, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Backing up PostgreSQL database: {DatabaseName}", backupJob.DatabaseSettings?.DatabaseName);
            
            if (backupJob.DatabaseSettings == null)
            {
                throw new ArgumentException("Database settings are required for database backup");
            }
            
            // Generate output file path
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var outputFilePath = Path.Combine(tempDir, $"{backupJob.DatabaseSettings.DatabaseName}_{timestamp}.dump");
            
            // Perform database backup
            outputFilePath = await _databaseBackupService.BackupPostgreSqlDatabaseAsync(backupJob.DatabaseSettings, outputFilePath);
            
            // Process the file (compress/encrypt)
            outputFilePath = await ProcessBackupFileAsync(outputFilePath, backupJob.Compress, backupJob.Encrypt);
            
            return outputFilePath;
        }

        private async Task<string> BackupMongoDbDatabaseAsync(BackupJobModel backupJob, string tempDir, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Backing up MongoDB database: {DatabaseName}", backupJob.DatabaseSettings?.DatabaseName);
            
            if (backupJob.DatabaseSettings == null)
            {
                throw new ArgumentException("Database settings are required for database backup");
            }
            
            // Generate output file path
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var outputFilePath = Path.Combine(tempDir, $"{backupJob.DatabaseSettings.DatabaseName}_{timestamp}");
            
            // Perform database backup
            outputFilePath = await _databaseBackupService.BackupMongoDbDatabaseAsync(backupJob.DatabaseSettings, outputFilePath);
            
            // Process the file (compress/encrypt)
            outputFilePath = await ProcessBackupFileAsync(outputFilePath, backupJob.Compress, backupJob.Encrypt);
            
            return outputFilePath;
        }

        private async Task<string> ProcessBackupFileAsync(string filePath, bool compress, bool encrypt)
        {
            var processedFilePath = filePath;
            
            // Compress if needed
            if (compress)
            {
                var compressedFilePath = $"{filePath}.zip";
                await _compressionService.CompressFileAsync(filePath, compressedFilePath);
                await _fileSystemService.DeleteFileAsync(filePath);
                processedFilePath = compressedFilePath;
            }
            
            // Encrypt if needed
            if (encrypt && _settings.EnableEncryption)
            {
                var encryptedFilePath = $"{processedFilePath}.enc";
                await _encryptionService.EncryptFileAsync(processedFilePath, encryptedFilePath, _settings.EncryptionKey);
                await _fileSystemService.DeleteFileAsync(processedFilePath);
                processedFilePath = encryptedFilePath;
            }
            
            return processedFilePath;
        }

        #endregion
    }
}