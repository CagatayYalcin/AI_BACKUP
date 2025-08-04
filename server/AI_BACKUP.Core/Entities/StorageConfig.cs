using System;
using System.Collections.Generic;

namespace AI_BACKUP.Core.Entities
{
    public class StorageConfig
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public StorageType StorageType { get; set; }
        public string Config { get; set; } // JSON configuration for the storage
        public int OwnerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public User Owner { get; set; }
        public ICollection<BackupJob> BackupJobs { get; set; }
    }

    public enum StorageType
    {
        Local,
        AwsS3,
        AzureBlob,
        GoogleCloud,
        GoogleDrive,
        OneDrive
    }
}