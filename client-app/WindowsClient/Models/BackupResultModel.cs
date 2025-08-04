namespace AI_BACKUP.WindowsService.Models
{
    public class BackupResultModel
    {
        public Guid BackupJobId { get; set; }
        public Guid BackupRunId { get; set; }
        public BackupRunStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public long SizeBytes { get; set; }
        public string BackupPath { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public string? LogPath { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public List<BackupFileInfo>? Files { get; set; }
    }

    public enum BackupRunStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Cancelled
    }

    public class BackupFileInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public DateTime LastModified { get; set; }
        public string? Checksum { get; set; }
    }
}