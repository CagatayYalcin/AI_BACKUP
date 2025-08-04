using AI_BACKUP.WindowsService.Models;

namespace AI_BACKUP.WindowsService.Services
{
    public interface ICloudStorageService
    {
        StorageType StorageType { get; }
        Task<bool> AuthenticateAsync(StorageConfigModel config);
        Task<string> UploadFileAsync(string localFilePath, StorageConfigModel config, string remotePath);
        Task<bool> DownloadFileAsync(string remotePath, StorageConfigModel config, string localFilePath);
        Task<bool> DeleteFileAsync(string remotePath, StorageConfigModel config);
        Task<List<string>> ListFilesAsync(string remotePath, StorageConfigModel config);
        Task<bool> TestConnectionAsync(StorageConfigModel config);
    }
}