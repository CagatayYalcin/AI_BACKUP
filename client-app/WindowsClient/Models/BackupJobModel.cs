namespace AI_BACKUP.WindowsService.Models
{
    public class BackupJobModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public BackupType BackupType { get; set; }
        public BackupJobStatus Status { get; set; }
        public string SourcePath { get; set; } = string.Empty;
        public bool Recursive { get; set; }
        public string FileFilter { get; set; } = string.Empty;
        public bool Compress { get; set; }
        public bool Encrypt { get; set; }
        public int RetentionDays { get; set; }
        public string Schedule { get; set; } = string.Empty;
        public DateTime NextRunTime { get; set; }
        public Guid StorageConfigId { get; set; }
        public DatabaseSettings? DatabaseSettings { get; set; }
    }

    public enum BackupType
    {
        File,
        Directory,
        MSSQLDatabase,
        MySQLDatabase,
        PostgreSQLDatabase,
        MongoDBDatabase
    }

    public enum BackupJobStatus
    {
        Active,
        Paused,
        Disabled,
        Running,
        Failed,
        Completed
    }

    public class DatabaseSettings
    {
        public string ServerName { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int Port { get; set; }
        public string ConnectionString { get; set; } = string.Empty;
        public bool UseIntegratedSecurity { get; set; }
        public bool IncludeSchema { get; set; } = true;
        public bool IncludeData { get; set; } = true;
    }
}