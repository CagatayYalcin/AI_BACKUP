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
        public string? Message { get; set; }
        public string? LogPath { get; set; }
        
        // Versioning properties
        public int TotalFiles { get; set; }
        public int ChangedFiles { get; set; }
        public int UnchangedFiles { get; set; }
        public long DeltaSize { get; set; }
        public List<string> FailedFiles { get; set; } = new List<string>();
        
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
        public bool HasChanged { get; set; }
        public BackupType BackupType { get; set; }
        public string? BaseVersionId { get; set; }
    }
}