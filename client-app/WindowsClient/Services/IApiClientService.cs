using AI_BACKUP.WindowsService.Models;

namespace AI_BACKUP.WindowsService.Services
{
    public interface IApiClientService
    {
        Task<bool> RegisterClientAsync();
        Task<bool> SendHeartbeatAsync(Dictionary<string, object> systemInfo);
        Task<List<BackupJobModel>> GetPendingBackupJobsAsync();
        Task<bool> UpdateBackupJobStatusAsync(Guid backupJobId, string status, string? message = null);
        Task<bool> UploadBackupResultAsync(BackupResultModel result);
        Task<StorageConfigModel?> GetStorageConfigAsync(Guid storageConfigId);
        Task<bool> AuthenticateAsync();
        Task<bool> IsAuthenticated();
    }
}