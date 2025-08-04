using AI_BACKUP.WindowsService.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;

namespace AI_BACKUP.WindowsService.Services
{
    public class FtpService : ICloudStorageService
    {
        private readonly ILogger<FtpService> _logger;
        private readonly ServiceSettings _settings;
        private readonly IFileSystemService _fileSystemService;

        public FtpService(
            ILogger<FtpService> logger,
            IOptions<ServiceSettings> settings,
            IFileSystemService fileSystemService)
        {
            _logger = logger;
            _settings = settings.Value;
            _fileSystemService = fileSystemService;
        }

        public StorageType StorageType => StorageType.FTP;

        public async Task<bool> AuthenticateAsync(StorageConfigModel config)
        {
            try
            {
                _logger.LogInformation("Testing authentication with FTP server: {Server}", config.ServerName);

                // Validate configuration
                if (string.IsNullOrEmpty(config.ServerName))
                {
                    throw new ArgumentException("FTP server name is required");
                }

                // Create test request
                var ftpUrl = BuildFtpUrl(config, "/");
                var request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                
                // Set credentials
                if (!string.IsNullOrEmpty(config.Username))
                {
                    request.Credentials = new NetworkCredential(config.Username, config.SecretKey);
                }
                
                // Set timeout
                request.Timeout = 10000; // 10 seconds
                
                // Execute request
                using var response = (FtpWebResponse)await request.GetResponseAsync();
                
                _logger.LogInformation("Successfully authenticated with FTP server: {Server}", config.ServerName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating with FTP server: {Server}", config.ServerName);
                return false;
            }
        }

        public async Task<string> UploadFileAsync(string localFilePath, StorageConfigModel config, string remotePath)
        {
            try
            {
                _logger.LogInformation("Uploading file to FTP server: {LocalFilePath} -> {RemotePath}", localFilePath, remotePath);

                // Validate configuration
                if (string.IsNullOrEmpty(config.ServerName))
                {
                    throw new ArgumentException("FTP server name is required");
                }

                // Create FTP request
                var ftpUrl = BuildFtpUrl(config, remotePath);
                var request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                
                // Set credentials
                if (!string.IsNullOrEmpty(config.Username))
                {
                    request.Credentials = new NetworkCredential(config.Username, config.SecretKey);
                }
                
                // Set binary mode
                request.UseBinary = true;
                request.UsePassive = true;
                request.KeepAlive = false;
                
                // Upload file
                using (var fileStream = File.OpenRead(localFilePath))
                using (var requestStream = await request.GetRequestStreamAsync())
                {
                    await fileStream.CopyToAsync(requestStream);
                }
                
                // Get response
                using var response = (FtpWebResponse)await request.GetResponseAsync();
                
                _logger.LogInformation("File uploaded successfully to FTP server. Status: {Status}", response.StatusDescription);
                return remotePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file to FTP server: {LocalFilePath}", localFilePath);
                throw;
            }
        }

        public async Task<bool> DownloadFileAsync(string remotePath, StorageConfigModel config, string localFilePath)
        {
            try
            {
                _logger.LogInformation("Downloading file from FTP server: {RemotePath} -> {LocalFilePath}", remotePath, localFilePath);

                // Validate configuration
                if (string.IsNullOrEmpty(config.ServerName))
                {
                    throw new ArgumentException("FTP server name is required");
                }

                // Ensure the directory exists
                var directory = Path.GetDirectoryName(localFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Create FTP request
                var ftpUrl = BuildFtpUrl(config, remotePath);
                var request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                
                // Set credentials
                if (!string.IsNullOrEmpty(config.Username))
                {
                    request.Credentials = new NetworkCredential(config.Username, config.SecretKey);
                }
                
                // Set binary mode
                request.UseBinary = true;
                request.UsePassive = true;
                request.KeepAlive = false;
                
                // Download file
                using (var response = (FtpWebResponse)await request.GetResponseAsync())
                using (var responseStream = response.GetResponseStream())
                using (var fileStream = File.Create(localFilePath))
                {
                    await responseStream.CopyToAsync(fileStream);
                }
                
                _logger.LogInformation("File downloaded successfully from FTP server");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file from FTP server: {RemotePath}", remotePath);
                return false;
            }
        }

        public async Task<bool> DeleteFileAsync(string remotePath, StorageConfigModel config)
        {
            try
            {
                _logger.LogInformation("Deleting file from FTP server: {RemotePath}", remotePath);

                // Validate configuration
                if (string.IsNullOrEmpty(config.ServerName))
                {
                    throw new ArgumentException("FTP server name is required");
                }

                // Create FTP request
                var ftpUrl = BuildFtpUrl(config, remotePath);
                var request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Method = WebRequestMethods.Ftp.DeleteFile;
                
                // Set credentials
                if (!string.IsNullOrEmpty(config.Username))
                {
                    request.Credentials = new NetworkCredential(config.Username, config.SecretKey);
                }
                
                // Execute request
                using var response = (FtpWebResponse)await request.GetResponseAsync();
                
                _logger.LogInformation("File deleted successfully from FTP server. Status: {Status}", response.StatusDescription);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file from FTP server: {RemotePath}", remotePath);
                return false;
            }
        }

        public async Task<List<string>> ListFilesAsync(string remotePath, StorageConfigModel config)
        {
            try
            {
                _logger.LogInformation("Listing files from FTP server: {RemotePath}", remotePath);

                // Validate configuration
                if (string.IsNullOrEmpty(config.ServerName))
                {
                    throw new ArgumentException("FTP server name is required");
                }

                var result = new List<string>();
                
                // Create FTP request
                var ftpUrl = BuildFtpUrl(config, remotePath);
                var request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                
                // Set credentials
                if (!string.IsNullOrEmpty(config.Username))
                {
                    request.Credentials = new NetworkCredential(config.Username, config.SecretKey);
                }
                
                // Execute request
                using (var response = (FtpWebResponse)await request.GetResponseAsync())
                using (var responseStream = response.GetResponseStream())
                using (var reader = new StreamReader(responseStream))
                {
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(line) && line != "." && line != "..")
                        {
                            result.Add(line);
                        }
                    }
                }
                
                _logger.LogInformation("Listed {Count} files from FTP server", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing files from FTP server: {RemotePath}", remotePath);
                return new List<string>();
            }
        }

        public async Task<bool> TestConnectionAsync(StorageConfigModel config)
        {
            return await AuthenticateAsync(config);
        }

        private string BuildFtpUrl(StorageConfigModel config, string path)
        {
            var builder = new StringBuilder();
            
            // Add protocol
            builder.Append("ftp://");
            
            // Add server
            builder.Append(config.ServerName);
            
            // Add port if specified
            if (config.Port > 0)
            {
                builder.Append($":{config.Port}");
            }
            
            // Add path
            if (!string.IsNullOrEmpty(path))
            {
                // Ensure path starts with /
                if (!path.StartsWith("/"))
                {
                    builder.Append("/");
                }
                
                builder.Append(path);
            }
            
            return builder.ToString();
        }
    }
}