using System;
using System.Collections.Generic;

namespace AI_BACKUP.Core.Entities
{
    public class BackupJob
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public BackupType BackupType { get; set; }
        public string SourcePath { get; set; } // File/folder path or database connection string
        public string Schedule { get; set; } // Cron expression
        public bool Encrypt { get; set; }
        public bool Compress { get; set; }
        public int RetentionDays { get; set; }
        public int OwnerId { get; set; }
        public int ClientId { get; set; }
        public int StorageConfigId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public User Owner { get; set; }
        public Client Client { get; set; }
        public StorageConfig StorageConfig { get; set; }
        public ICollection<BackupRun> BackupRuns { get; set; }
    }

    public enum BackupType
    {
        File,
        Folder,
        MsSql,
        MySql,
        PostgreSql
    }
}