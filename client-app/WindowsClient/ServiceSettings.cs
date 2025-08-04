namespace AI_BACKUP.WindowsService
{
    public class ServiceSettings
    {
        public string ApiBaseUrl { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public int HeartbeatIntervalSeconds { get; set; } = 60;
        public int BackupCheckIntervalMinutes { get; set; } = 5;
        public string TempDirectory { get; set; } = string.Empty;
        public string LogDirectory { get; set; } = string.Empty;
        public bool EnableEncryption { get; set; } = true;
        public string EncryptionKey { get; set; } = string.Empty;
        public int MaxConcurrentBackups { get; set; } = 2;
        public int MaxRetryAttempts { get; set; } = 3;
        public int RetryDelaySeconds { get; set; } = 30;
    }
}