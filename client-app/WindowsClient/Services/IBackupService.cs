using AI_BACKUP.WindowsService.Models;

namespace AI_BACKUP.WindowsService.Services
{
    public interface IBackupService
    {
        Task<BackupResultModel> PerformBackupAsync(BackupJobModel backupJob, CancellationToken cancellationToken);
        Task<bool> VerifyBackupAsync(BackupResultModel backupResult);
        Task<bool> RestoreBackupAsync(Guid backupRunId, string destinationPath);
    }
}