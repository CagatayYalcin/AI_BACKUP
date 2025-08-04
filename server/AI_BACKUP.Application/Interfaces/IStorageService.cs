using AI_BACKUP.Application.DTOs;
using AI_BACKUP.Core.Entities;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AI_BACKUP.Application.Interfaces
{
    public interface IStorageService
    {
        Task<StorageConfigDto> GetStorageConfigByIdAsync(int id);
        Task<IReadOnlyList<StorageConfigDto>> GetAllStorageConfigsAsync();
        Task<IReadOnlyList<StorageConfigDto>> GetStorageConfigsByOwnerIdAsync(int ownerId);
        Task<StorageConfigDto> CreateStorageConfigAsync(int ownerId, CreateStorageConfigDto createStorageConfigDto);
        Task UpdateStorageConfigAsync(int id, UpdateStorageConfigDto updateStorageConfigDto);
        Task DeleteStorageConfigAsync(int id);
        
        // Storage provider operations
        Task<string> UploadFileAsync(int storageConfigId, Stream fileStream, string fileName, string contentType);
        Task<Stream> DownloadFileAsync(int storageConfigId, string filePath);
        Task DeleteFileAsync(int storageConfigId, string filePath);
        Task<IEnumerable<string>> ListFilesAsync(int storageConfigId, string prefix);
    }
}