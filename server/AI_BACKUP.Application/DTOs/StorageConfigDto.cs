using AI_BACKUP.Core.Entities;
using System;

namespace AI_BACKUP.Application.DTOs
{
    public class StorageConfigDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public StorageType StorageType { get; set; }
        public string Config { get; set; }
        public int OwnerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateStorageConfigDto
    {
        public string Name { get; set; }
        public StorageType StorageType { get; set; }
        public string Config { get; set; }
    }

    public class UpdateStorageConfigDto
    {
        public string Name { get; set; }
        public string Config { get; set; }
    }
}