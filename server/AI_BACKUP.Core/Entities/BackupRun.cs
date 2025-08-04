using System;

namespace AI_BACKUP.Core.Entities
{
    public class BackupRun
    {
        public int Id { get; set; }
        public int BackupJobId { get; set; }
        public BackupStatus Status { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public long? SizeBytes { get; set; }
        public int? FileCount { get; set; }
        public string ErrorMessage { get; set; }
        public string DestinationPath { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public BackupJob BackupJob { get; set; }
    }

    public enum BackupStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed
    }
}