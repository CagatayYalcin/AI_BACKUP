using AI_BACKUP.WindowsService.Models;

namespace AI_BACKUP.WindowsService.Services
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(string filePath, StorageConfigModel storageConfig, string remotePath);
        Task<bool> DownloadFileAsync(string remotePath, StorageConfigModel storageConfig, string localPath);
        Task<bool> DeleteFileAsync(string remotePath, StorageConfigModel storageConfig);
        Task<List<string>> ListFilesAsync(string remotePath, StorageConfigModel storageConfig);
        Task<bool> TestConnectionAsync(StorageConfigModel storageConfig);
    }
}