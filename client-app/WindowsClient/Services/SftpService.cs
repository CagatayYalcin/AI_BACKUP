using AI_BACKUP.WindowsService.Models;
using Microsoft.Extensions.Options;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using System.Text;

namespace AI_BACKUP.WindowsService.Services
{
    public class SftpService : ICloudStorageService
    {
        private readonly ILogger<SftpService> _logger;
        private readonly ServiceSettings _settings;
        private readonly IFileSystemService _fileSystemService;

        public SftpService(
            ILogger<SftpService> logger,
            IOptions<ServiceSettings> settings,
            IFileSystemService fileSystemService)
        {
            _logger = logger;
            _settings = settings.Value;
            _fileSystemService = fileSystemService;
        }

        public StorageType StorageType => StorageType.SFTP;

        public async Task<bool> AuthenticateAsync(StorageConfigModel config)
        {
            try
            {
                _logger.LogInformation("Testing authentication with SFTP server: {Server}", config.ServerName);

                // Validate configuration
                if (string.IsNullOrEmpty(config.ServerName))
                {
                    throw new ArgumentException("SFTP server name is required");
                }

                if (string.IsNullOrEmpty(config.Username))
                {
                    throw new ArgumentException("SFTP username is required");
                }

                // Create connection info
                ConnectionInfo connectionInfo;
                
                // Check authentication method
                if (!string.IsNullOrEmpty(config.PrivateKeyPath))
                {
                    // Use private key authentication
                    var privateKeyFile = new PrivateKeyFile(config.PrivateKeyPath, config.SecretKey);
                    var privateKeyAuthMethod = new PrivateKeyAuthenticationMethod(config.Username, privateKeyFile);
                    
                    connectionInfo = new ConnectionInfo(
                        config.ServerName,
                        config.Port > 0 ? config.Port : 22,
                        config.Username,
                        privateKeyAuthMethod);
                }
                else
                {
                    // Use password authentication
                    var passwordAuthMethod = new PasswordAuthenticationMethod(config.Username, config.SecretKey);
                    
                    connectionInfo = new ConnectionInfo(
                        config.ServerName,
                        config.Port > 0 ? config.Port : 22,
                        config.Username,
                        passwordAuthMethod);
                }
                
                // Connect to server
                using var client = new SftpClient(connectionInfo);
                client.Connect();
                
                if (client.IsConnected)
                {
                    client.Disconnect();
                    _logger.LogInformation("Successfully authenticated with SFTP server: {Server}", config.ServerName);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Failed to connect to SFTP server: {Server}", config.ServerName);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating with SFTP server: {Server}", config.ServerName);
                return false;
            }
        }

        public async Task<string> UploadFileAsync(string localFilePath, StorageConfigModel config, string remotePath)
        {
            try
            {
                _logger.LogInformation("Uploading file to SFTP server: {LocalFilePath} -> {RemotePath}", localFilePath, remotePath);

                // Validate configuration
                if (string.IsNullOrEmpty(config.ServerName))
                {
                    throw new ArgumentException("SFTP server name is required");
                }

                if (string.IsNullOrEmpty(config.Username))
                {
                    throw new ArgumentException("SFTP username is required");
                }

                // Create connection info
                ConnectionInfo connectionInfo;
                
                // Check authentication method
                if (!string.IsNullOrEmpty(config.PrivateKeyPath))
                {
                    // Use private key authentication
                    var privateKeyFile = new PrivateKeyFile(config.PrivateKeyPath, config.SecretKey);
                    var privateKeyAuthMethod = new PrivateKeyAuthenticationMethod(config.Username, privateKeyFile);
                    
                    connectionInfo = new ConnectionInfo(
                        config.ServerName,
                        config.Port > 0 ? config.Port : 22,
                        config.Username,
                        privateKeyAuthMethod);
                }
                else
                {
                    // Use password authentication
                    var passwordAuthMethod = new PasswordAuthenticationMethod(config.Username, config.SecretKey);
                    
                    connectionInfo = new ConnectionInfo(
                        config.ServerName,
                        config.Port > 0 ? config.Port : 22,
                        config.Username,
                        passwordAuthMethod);
                }
                
                // Connect to server
                using var client = new SftpClient(connectionInfo);
                client.Connect();
                
                if (!client.IsConnected)
                {
                    throw new Exception($"Failed to connect to SFTP server: {config.ServerName}");
                }
                
                // Ensure remote directory exists
                var remoteDir = Path.GetDirectoryName(remotePath)?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(remoteDir))
                {
                    await EnsureDirectoryExistsAsync(client, remoteDir);
                }
                
                // Upload file
                using var fileStream = File.OpenRead(localFilePath);
                client.UploadFile(fileStream, remotePath);
                
                client.Disconnect();
                
                _logger.LogInformation("File uploaded successfully to SFTP server");
                return remotePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file to SFTP server: {LocalFilePath}", localFilePath);
                throw;
            }
        }

        public async Task<bool> DownloadFileAsync(string remotePath, StorageConfigModel config, string localFilePath)
        {
            try
            {
                _logger.LogInformation("Downloading file from SFTP server: {RemotePath} -> {LocalFilePath}", remotePath, localFilePath);

                // Validate configuration
                if (string.IsNullOrEmpty(config.ServerName))
                {
                    throw new ArgumentException("SFTP server name is required");
                }

                if (string.IsNullOrEmpty(config.Username))
                {
                    throw new ArgumentException("SFTP username is required");
                }

                // Ensure the directory exists
                var directory = Path.GetDirectoryName(localFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Create connection info
                ConnectionInfo connectionInfo;
                
                // Check authentication method
                if (!string.IsNullOrEmpty(config.PrivateKeyPath))
                {
                    // Use private key authentication
                    var privateKeyFile = new PrivateKeyFile(config.PrivateKeyPath, config.SecretKey);
                    var privateKeyAuthMethod = new PrivateKeyAuthenticationMethod(config.Username, privateKeyFile);
                    
                    connectionInfo = new ConnectionInfo(
                        config.ServerName,
                        config.Port > 0 ? config.Port : 22,
                        config.Username,
                        privateKeyAuthMethod);
                }
                else
                {
                    // Use password authentication
                    var passwordAuthMethod = new PasswordAuthenticationMethod(config.Username, config.SecretKey);
                    
                    connectionInfo = new ConnectionInfo(
                        config.ServerName,
                        config.Port > 0 ? config.Port : 22,
                        config.Username,
                        passwordAuthMethod);
                }
                
                // Connect to server
                using var client = new SftpClient(connectionInfo);
                client.Connect();
                
                if (!client.IsConnected)
                {
                    throw new Exception($"Failed to connect to SFTP server: {config.ServerName}");
                }
                
                // Download file
                using var fileStream = File.Create(localFilePath);
                client.DownloadFile(remotePath, fileStream);
                
                client.Disconnect();
                
                _logger.LogInformation("File downloaded successfully from SFTP server");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file from SFTP server: {RemotePath}", remotePath);
                return false;
            }
        }

        public async Task<bool> DeleteFileAsync(string remotePath, StorageConfigModel config)
        {
            try
            {
                _logger.LogInformation("Deleting file from SFTP server: {RemotePath}", remotePath);

                // Validate configuration
                if (string.IsNullOrEmpty(config.ServerName))
                {
                    throw new ArgumentException("SFTP server name is required");
                }

                if (string.IsNullOrEmpty(config.Username))
                {
                    throw new ArgumentException("SFTP username is required");
                }

                // Create connection info
                ConnectionInfo connectionInfo;
                
                // Check authentication method
                if (!string.IsNullOrEmpty(config.PrivateKeyPath))
                {
                    // Use private key authentication
                    var privateKeyFile = new PrivateKeyFile(config.PrivateKeyPath, config.SecretKey);
                    var privateKeyAuthMethod = new PrivateKeyAuthenticationMethod(config.Username, privateKeyFile);
                    
                    connectionInfo = new ConnectionInfo(
                        config.ServerName,
                        config.Port > 0 ? config.Port : 22,
                        config.Username,
                        privateKeyAuthMethod);
                }
                else
                {
                    // Use password authentication
                    var passwordAuthMethod = new PasswordAuthenticationMethod(config.Username, config.SecretKey);
                    
                    connectionInfo = new ConnectionInfo(
                        config.ServerName,
                        config.Port > 0 ? config.Port : 22,
                        config.Username,
                        passwordAuthMethod);
                }
                
                // Connect to server
                using var client = new SftpClient(connectionInfo);
                client.Connect();
                
                if (!client.IsConnected)
                {
                    throw new Exception($"Failed to connect to SFTP server: {config.ServerName}");
                }
                
                // Delete file
                client.DeleteFile(remotePath);
                
                client.Disconnect();
                
                _logger.LogInformation("File deleted successfully from SFTP server");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file from SFTP server: {RemotePath}", remotePath);
                return false;
            }
        }

        public async Task<List<string>> ListFilesAsync(string remotePath, StorageConfigModel config)
        {
            try
            {
                _logger.LogInformation("Listing files from SFTP server: {RemotePath}", remotePath);

                // Validate configuration
                if (string.IsNullOrEmpty(config.ServerName))
                {
                    throw new ArgumentException("SFTP server name is required");
                }

                if (string.IsNullOrEmpty(config.Username))
                {
                    throw new ArgumentException("SFTP username is required");
                }

                var result = new List<string>();
                
                // Create connection info
                ConnectionInfo connectionInfo;
                
                // Check authentication method
                if (!string.IsNullOrEmpty(config.PrivateKeyPath))
                {
                    // Use private key authentication
                    var privateKeyFile = new PrivateKeyFile(config.PrivateKeyPath, config.SecretKey);
                    var privateKeyAuthMethod = new PrivateKeyAuthenticationMethod(config.Username, privateKeyFile);
                    
                    connectionInfo = new ConnectionInfo(
                        config.ServerName,
                        config.Port > 0 ? config.Port : 22,
                        config.Username,
                        privateKeyAuthMethod);
                }
                else
                {
                    // Use password authentication
                    var passwordAuthMethod = new PasswordAuthenticationMethod(config.Username, config.SecretKey);
                    
                    connectionInfo = new ConnectionInfo(
                        config.ServerName,
                        config.Port > 0 ? config.Port : 22,
                        config.Username,
                        passwordAuthMethod);
                }
                
                // Connect to server
                using var client = new SftpClient(connectionInfo);
                client.Connect();
                
                if (!client.IsConnected)
                {
                    throw new Exception($"Failed to connect to SFTP server: {config.ServerName}");
                }
                
                // List files
                var path = string.IsNullOrEmpty(remotePath) ? "." : remotePath;
                var files = client.ListDirectory(path);
                
                foreach (var file in files)
                {
                    if (!file.IsDirectory && file.Name != "." && file.Name != "..")
                    {
                        result.Add(file.FullName);
                    }
                }
                
                client.Disconnect();
                
                _logger.LogInformation("Listed {Count} files from SFTP server", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing files from SFTP server: {RemotePath}", remotePath);
                return new List<string>();
            }
        }

        public async Task<bool> TestConnectionAsync(StorageConfigModel config)
        {
            return await AuthenticateAsync(config);
        }

        private async Task EnsureDirectoryExistsAsync(SftpClient client, string path)
        {
            // Split path into segments
            var segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var currentPath = string.Empty;
            
            // Create each directory in the path if it doesn't exist
            foreach (var segment in segments)
            {
                currentPath += "/" + segment;
                
                if (!client.Exists(currentPath))
                {
                    client.CreateDirectory(currentPath);
                }
            }
        }
    }
}