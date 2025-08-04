using AI_BACKUP.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AI_BACKUP.Application.Interfaces
{
    public interface IBackupJobService
    {
        Task<BackupJobDto> GetBackupJobByIdAsync(int id);
        Task<IReadOnlyList<BackupJobDto>> GetAllBackupJobsAsync();
        Task<IReadOnlyList<BackupJobDto>> GetBackupJobsByOwnerIdAsync(int ownerId);
        Task<IReadOnlyList<BackupJobDto>> GetBackupJobsByClientIdAsync(int clientId);
        Task<BackupJobDto> CreateBackupJobAsync(int ownerId, CreateBackupJobDto createBackupJobDto);
        Task UpdateBackupJobAsync(int id, UpdateBackupJobDto updateBackupJobDto);
        Task DeleteBackupJobAsync(int id);
    }
}