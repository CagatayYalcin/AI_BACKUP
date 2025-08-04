using AI_BACKUP.Core.Entities;
using System;

namespace AI_BACKUP.Application.DTOs
{
    public class BackupJobDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public BackupType BackupType { get; set; }
        public string SourcePath { get; set; }
        public string Schedule { get; set; }
        public bool Encrypt { get; set; }
        public bool Compress { get; set; }
        public int RetentionDays { get; set; }
        public int OwnerId { get; set; }
        public int ClientId { get; set; }
        public int StorageConfigId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateBackupJobDto
    {
        public string Name { get; set; }
        public BackupType BackupType { get; set; }
        public string SourcePath { get; set; }
        public string Schedule { get; set; }
        public bool Encrypt { get; set; }
        public bool Compress { get; set; }
        public int RetentionDays { get; set; }
        public int ClientId { get; set; }
        public int StorageConfigId { get; set; }
    }

    public class UpdateBackupJobDto
    {
        public string Name { get; set; }
        public string SourcePath { get; set; }
        public string Schedule { get; set; }
        public bool Encrypt { get; set; }
        public bool Compress { get; set; }
        public int RetentionDays { get; set; }
        public int StorageConfigId { get; set; }
    }
}