using AI_BACKUP.WindowsService.Models;
using Microsoft.Extensions.Options;

namespace AI_BACKUP.WindowsService.Services
{
    public class StorageService : IStorageService
    {
        private readonly ILogger<StorageService> _logger;
        private readonly ServiceSettings _settings;
        private readonly IFileSystemService _fileSystemService;
        private readonly IEnumerable<ICloudStorageService> _cloudStorageServices;
        private readonly Dictionary<StorageType, ICloudStorageService> _cloudStorageServiceMap;

        public StorageService(
            ILogger<StorageService> logger,
            IOptions<ServiceSettings> settings,
            IFileSystemService fileSystemService,
            IEnumerable<ICloudStorageService> cloudStorageServices)
        {
            _logger = logger;
            _settings = settings.Value;
            _fileSystemService = fileSystemService;
            _cloudStorageServices = cloudStorageServices;
            
            // Create a map of storage type to service for easy lookup
            _cloudStorageServiceMap = _cloudStorageServices.ToDictionary(s => s.StorageType);
        }

        public async Task<string> UploadFileAsync(string filePath, StorageConfigModel storageConfig, string remotePath)
        {
            try
            {
                _logger.LogInformation("Uploading file {FilePath} to {StorageType} storage at {RemotePath}", 
                    filePath, storageConfig.StorageType, remotePath);
                
                if (storageConfig.StorageType == StorageType.Local)
                {
                    return await UploadToLocalStorageAsync(filePath, storageConfig, remotePath);
                }
                else if (_cloudStorageServiceMap.TryGetValue(storageConfig.StorageType, out var cloudService))
                {
                    return await cloudService.UploadFileAsync(filePath, storageConfig, remotePath);
                }
                else
                {
                    throw new NotSupportedException($"Storage type {storageConfig.StorageType} is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {FilePath} to {StorageType} storage", 
                    filePath, storageConfig.StorageType);
                throw;
            }
        }

        public async Task<bool> DownloadFileAsync(string remotePath, StorageConfigModel storageConfig, string localPath)
        {
            try
            {
                _logger.LogInformation("Downloading file from {StorageType} storage at {RemotePath} to {LocalPath}", 
                    storageConfig.StorageType, remotePath, localPath);
                
                if (storageConfig.StorageType == StorageType.Local)
                {
                    return await DownloadFromLocalStorageAsync(remotePath, storageConfig, localPath);
                }
                else if (_cloudStorageServiceMap.TryGetValue(storageConfig.StorageType, out var cloudService))
                {
                    return await cloudService.DownloadFileAsync(remotePath, storageConfig, localPath);
                }
                else
                {
                    throw new NotSupportedException($"Storage type {storageConfig.StorageType} is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file from {StorageType} storage at {RemotePath}", 
                    storageConfig.StorageType, remotePath);
                return false;
            }
        }

        public async Task<bool> DeleteFileAsync(string remotePath, StorageConfigModel storageConfig)
        {
            try
            {
                _logger.LogInformation("Deleting file from {StorageType} storage at {RemotePath}", 
                    storageConfig.StorageType, remotePath);
                
                if (storageConfig.StorageType == StorageType.Local)
                {
                    return await DeleteFromLocalStorageAsync(remotePath, storageConfig);
                }
                else if (_cloudStorageServiceMap.TryGetValue(storageConfig.StorageType, out var cloudService))
                {
                    return await cloudService.DeleteFileAsync(remotePath, storageConfig);
                }
                else
                {
                    throw new NotSupportedException($"Storage type {storageConfig.StorageType} is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file from {StorageType} storage at {RemotePath}", 
                    storageConfig.StorageType, remotePath);
                return false;
            }
        }

        public async Task<List<string>> ListFilesAsync(string remotePath, StorageConfigModel storageConfig)
        {
            try
            {
                _logger.LogInformation("Listing files from {StorageType} storage at {RemotePath}", 
                    storageConfig.StorageType, remotePath);
                
                if (storageConfig.StorageType == StorageType.Local)
                {
                    return await ListFilesFromLocalStorageAsync(remotePath, storageConfig);
                }
                else if (_cloudStorageServiceMap.TryGetValue(storageConfig.StorageType, out var cloudService))
                {
                    return await cloudService.ListFilesAsync(remotePath, storageConfig);
                }
                else
                {
                    throw new NotSupportedException($"Storage type {storageConfig.StorageType} is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing files from {StorageType} storage at {RemotePath}", 
                    storageConfig.StorageType, remotePath);
                return new List<string>();
            }
        }

        public async Task<bool> TestConnectionAsync(StorageConfigModel storageConfig)
        {
            try
            {
                _logger.LogInformation("Testing connection to {StorageType} storage", storageConfig.StorageType);
                
                if (storageConfig.StorageType == StorageType.Local)
                {
                    return await TestLocalStorageConnectionAsync(storageConfig);
                }
                else if (_cloudStorageServiceMap.TryGetValue(storageConfig.StorageType, out var cloudService))
                {
                    return await cloudService.TestConnectionAsync(storageConfig);
                }
                else
                {
                    throw new NotSupportedException($"Storage type {storageConfig.StorageType} is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connection to {StorageType} storage", storageConfig.StorageType);
                return false;
            }
        }

        #region Local Storage Implementation

        private async Task<string> UploadToLocalStorageAsync(string filePath, StorageConfigModel storageConfig, string remotePath)
        {
            var destinationPath = Path.Combine(storageConfig.FolderPath ?? "", remotePath);
            await _fileSystemService.CopyFileAsync(filePath, destinationPath, true);
            return destinationPath;
        }

        private async Task<bool> DownloadFromLocalStorageAsync(string remotePath, StorageConfigModel storageConfig, string localPath)
        {
            var sourcePath = Path.Combine(storageConfig.FolderPath ?? "", remotePath);
            await _fileSystemService.CopyFileAsync(sourcePath, localPath, true);
            return true;
        }

        private async Task<bool> DeleteFromLocalStorageAsync(string remotePath, StorageConfigModel storageConfig)
        {
            var filePath = Path.Combine(storageConfig.FolderPath ?? "", remotePath);
            await _fileSystemService.DeleteFileAsync(filePath);
            return true;
        }

        private async Task<List<string>> ListFilesFromLocalStorageAsync(string remotePath, StorageConfigModel storageConfig)
        {
            var directoryPath = Path.Combine(storageConfig.FolderPath ?? "", remotePath);
            var files = await _fileSystemService.GetFilesAsync(directoryPath, "*.*", false);
            return files.Select(f => Path.GetFileName(f)).ToList();
        }

        private async Task<bool> TestLocalStorageConnectionAsync(StorageConfigModel storageConfig)
        {
            if (string.IsNullOrEmpty(storageConfig.FolderPath))
            {
                return false;
            }
            
            return await _fileSystemService.DirectoryExistsAsync(storageConfig.FolderPath);
        }

        #endregion
    }
}