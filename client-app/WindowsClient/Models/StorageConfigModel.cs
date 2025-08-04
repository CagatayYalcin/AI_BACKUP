namespace AI_BACKUP.WindowsService.Models
{
    public class StorageConfigModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public StorageType StorageType { get; set; }
        public bool IsDefault { get; set; }
        public bool IsEnabled { get; set; } = true;
        public string? ServerName { get; set; }
        public int Port { get; set; }
        public string? Username { get; set; }
        public string? ConnectionString { get; set; }
        public string? AccessKey { get; set; }
        public string? SecretKey { get; set; }
        public string? BucketName { get; set; }
        public string? ContainerName { get; set; }
        public string? FolderPath { get; set; }
        public string? Region { get; set; }
        public string? Endpoint { get; set; }
        public string? PrivateKeyPath { get; set; }
    }

    public enum StorageType
    {
        Local,
        GoogleDrive,
        OneDrive,
        AmazonS3,
        AzureBlob,
        FTP,
        SFTP
    }
}