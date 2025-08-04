using System;
using System.Collections.Generic;

namespace AI_BACKUP.WindowsService.Models
{
    /// <summary>
    /// Represents a version of a file in the backup system
    /// </summary>
    public class FileVersionModel
    {
        /// <summary>
        /// Unique identifier for the file version
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The backup job this file version belongs to
        /// </summary>
        public Guid BackupJobId { get; set; }

        /// <summary>
        /// The backup run this file version was created in
        /// </summary>
        public Guid BackupRunId { get; set; }

        /// <summary>
        /// Full path of the file on the source system
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// File name including extension
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Size of the file in bytes
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Hash of the file content (SHA-256)
        /// </summary>
        public string ContentHash { get; set; }

        /// <summary>
        /// Last modified date of the file
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Creation date of the file
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Path to the backup file in the storage
        /// </summary>
        public string BackupPath { get; set; }

        /// <summary>
        /// Indicates if this is a full backup or a delta backup
        /// </summary>
        public BackupType BackupType { get; set; }

        /// <summary>
        /// If this is a delta backup, the ID of the base version
        /// </summary>
        public Guid? BaseVersionId { get; set; }

        /// <summary>
        /// Indicates if the file was encrypted
        /// </summary>
        public bool IsEncrypted { get; set; }

        /// <summary>
        /// Indicates if the file was compressed
        /// </summary>
        public bool IsCompressed { get; set; }

        /// <summary>
        /// Additional metadata about the file
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Type of backup for a file
    /// </summary>
    public enum BackupType
    {
        /// <summary>
        /// Full backup of the file
        /// </summary>
        Full = 0,

        /// <summary>
        /// Delta backup containing only changes from the base version
        /// </summary>
        Delta = 1,

        /// <summary>
        /// Reference to an existing file (deduplication)
        /// </summary>
        Reference = 2
    }
}