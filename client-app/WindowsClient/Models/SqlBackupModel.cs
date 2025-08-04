using System;
using System.Collections.Generic;

namespace AI_BACKUP.WindowsService.Models
{
    /// <summary>
    /// Represents a SQL backup job configuration
    /// </summary>
    public class SqlBackupModel
    {
        /// <summary>
        /// Unique identifier for the backup job
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name of the backup job
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the backup job
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Database connection settings
        /// </summary>
        public DatabaseSettings DatabaseSettings { get; set; } = new DatabaseSettings();

        /// <summary>
        /// Type of SQL backup strategy to use
        /// </summary>
        public SqlBackupType BackupType { get; set; } = SqlBackupType.Full;

        /// <summary>
        /// Schedule for full backups
        /// </summary>
        public BackupSchedule FullBackupSchedule { get; set; } = new BackupSchedule();

        /// <summary>
        /// Schedule for differential backups (only used if BackupType is DifferentialWithLogs)
        /// </summary>
        public BackupSchedule DifferentialBackupSchedule { get; set; } = new BackupSchedule();

        /// <summary>
        /// Schedule for transaction log backups (only used if BackupType is DifferentialWithLogs)
        /// </summary>
        public BackupSchedule TransactionLogBackupSchedule { get; set; } = new BackupSchedule();

        /// <summary>
        /// Whether to compress the backup files
        /// </summary>
        public bool Compress { get; set; } = true;

        /// <summary>
        /// Whether to encrypt the backup files
        /// </summary>
        public bool Encrypt { get; set; } = false;

        /// <summary>
        /// Encryption key for the backup files (if encryption is enabled)
        /// </summary>
        public string? EncryptionKey { get; set; }

        /// <summary>
        /// Storage configuration ID for the backup destination
        /// </summary>
        public Guid StorageConfigId { get; set; }

        /// <summary>
        /// Destination path within the storage
        /// </summary>
        public string DestinationPath { get; set; } = string.Empty;

        /// <summary>
        /// Number of full backups to retain
        /// </summary>
        public int RetentionCount { get; set; } = 7;

        /// <summary>
        /// Number of days to retain backups
        /// </summary>
        public int RetentionDays { get; set; } = 30;

        /// <summary>
        /// Whether to verify the backup after creation
        /// </summary>
        public bool VerifyBackup { get; set; } = true;

        /// <summary>
        /// Whether to copy the backup to a secondary location
        /// </summary>
        public bool CopyToSecondaryLocation { get; set; } = false;

        /// <summary>
        /// Secondary storage configuration ID (if copying to secondary location)
        /// </summary>
        public Guid? SecondaryStorageConfigId { get; set; }

        /// <summary>
        /// Secondary destination path (if copying to secondary location)
        /// </summary>
        public string? SecondaryDestinationPath { get; set; }

        /// <summary>
        /// Whether the backup job is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Last time the backup job was run
        /// </summary>
        public DateTime? LastRunTime { get; set; }

        /// <summary>
        /// Status of the last backup run
        /// </summary>
        public BackupRunStatus? LastRunStatus { get; set; }

        /// <summary>
        /// Next scheduled run time
        /// </summary>
        public DateTime? NextRunTime { get; set; }
    }

    /// <summary>
    /// Type of SQL backup strategy
    /// </summary>
    public enum SqlBackupType
    {
        /// <summary>
        /// Full backups only
        /// </summary>
        Full = 0,

        /// <summary>
        /// Full and differential backups
        /// </summary>
        Differential = 1,

        /// <summary>
        /// Full, differential, and transaction log backups
        /// </summary>
        DifferentialWithLogs = 2
    }

    /// <summary>
    /// Represents a backup schedule
    /// </summary>
    public class BackupSchedule
    {
        /// <summary>
        /// Type of schedule
        /// </summary>
        public ScheduleType Type { get; set; } = ScheduleType.Daily;

        /// <summary>
        /// Start time of the schedule
        /// </summary>
        public TimeSpan StartTime { get; set; } = new TimeSpan(0, 0, 0);

        /// <summary>
        /// Interval in minutes (for interval schedule type)
        /// </summary>
        public int IntervalMinutes { get; set; } = 60;

        /// <summary>
        /// Days of the week to run (for weekly schedule type)
        /// </summary>
        public List<DayOfWeek> DaysOfWeek { get; set; } = new List<DayOfWeek> { DayOfWeek.Monday };

        /// <summary>
        /// Day of the month to run (for monthly schedule type)
        /// </summary>
        public int DayOfMonth { get; set; } = 1;
    }

    /// <summary>
    /// Type of backup schedule
    /// </summary>
    public enum ScheduleType
    {
        /// <summary>
        /// Run once at the specified time
        /// </summary>
        Once = 0,

        /// <summary>
        /// Run at the specified time every day
        /// </summary>
        Daily = 1,

        /// <summary>
        /// Run at the specified time on the specified days of the week
        /// </summary>
        Weekly = 2,

        /// <summary>
        /// Run at the specified time on the specified day of the month
        /// </summary>
        Monthly = 3,

        /// <summary>
        /// Run at the specified interval
        /// </summary>
        Interval = 4
    }

    /// <summary>
    /// Represents a SQL backup run
    /// </summary>
    public class SqlBackupRunModel
    {
        /// <summary>
        /// Unique identifier for the backup run
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// ID of the backup job
        /// </summary>
        public Guid BackupJobId { get; set; }

        /// <summary>
        /// Type of backup performed
        /// </summary>
        public SqlBackupRunType BackupType { get; set; }

        /// <summary>
        /// Start time of the backup
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// End time of the backup
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Status of the backup
        /// </summary>
        public BackupRunStatus Status { get; set; }

        /// <summary>
        /// Size of the backup file in bytes
        /// </summary>
        public long SizeBytes { get; set; }

        /// <summary>
        /// Path to the backup file
        /// </summary>
        public string BackupPath { get; set; } = string.Empty;

        /// <summary>
        /// Error message (if the backup failed)
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// For differential or log backups, the ID of the base full backup
        /// </summary>
        public Guid? BaseBackupId { get; set; }

        /// <summary>
        /// For log backups, the ID of the base differential backup (if any)
        /// </summary>
        public Guid? BaseDifferentialBackupId { get; set; }

        /// <summary>
        /// LSN (Log Sequence Number) at the start of the backup
        /// </summary>
        public string? StartLsn { get; set; }

        /// <summary>
        /// LSN (Log Sequence Number) at the end of the backup
        /// </summary>
        public string? EndLsn { get; set; }

        /// <summary>
        /// Whether the backup was verified
        /// </summary>
        public bool Verified { get; set; }

        /// <summary>
        /// Whether the backup was copied to a secondary location
        /// </summary>
        public bool CopiedToSecondaryLocation { get; set; }

        /// <summary>
        /// Path to the backup file in the secondary location
        /// </summary>
        public string? SecondaryBackupPath { get; set; }
    }

    /// <summary>
    /// Type of SQL backup run
    /// </summary>
    public enum SqlBackupRunType
    {
        /// <summary>
        /// Full backup
        /// </summary>
        Full = 0,

        /// <summary>
        /// Differential backup
        /// </summary>
        Differential = 1,

        /// <summary>
        /// Transaction log backup
        /// </summary>
        TransactionLog = 2
    }
}