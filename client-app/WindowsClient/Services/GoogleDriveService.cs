using AI_BACKUP.WindowsService.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Options;
using System.Text;

namespace AI_BACKUP.WindowsService.Services
{
    public class GoogleDriveService : ICloudStorageService
    {
        private readonly ILogger<GoogleDriveService> _logger;
        private readonly ServiceSettings _settings;
        private readonly IFileSystemService _fileSystemService;
        private DriveService? _driveService;

        public GoogleDriveService(
            ILogger<GoogleDriveService> logger,
            IOptions<ServiceSettings> settings,
            IFileSystemService fileSystemService)
        {
            _logger = logger;
            _settings = settings.Value;
            _fileSystemService = fileSystemService;
        }

        public StorageType StorageType => StorageType.GoogleDrive;

        public async Task<bool> AuthenticateAsync(StorageConfigModel config)
        {
            try
            {
                _logger.LogInformation("Authenticating with Google Drive");

                // Create credential directory if it doesn't exist
                var credentialDir = Path.Combine(_settings.TempDirectory, "GoogleDriveCredentials");
                if (!Directory.Exists(credentialDir))
                {
                    Directory.CreateDirectory(credentialDir);
                }

                // Check if we have client secrets
                if (string.IsNullOrEmpty(config.AccessKey) || string.IsNullOrEmpty(config.SecretKey))
                {
                    throw new ArgumentException("Google Drive client ID and client secret are required");
                }

                // Create client secrets from config
                var clientSecrets = new ClientSecrets
                {
                    ClientId = config.AccessKey,
                    ClientSecret = config.SecretKey
                };

                // Create credential store
                var credentialPath = Path.Combine(credentialDir, "token.json");

                // Request authorization
                var scopes = new[] { DriveService.Scope.DriveFile };
                var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    clientSecrets,
                    scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credentialPath, true));

                // Create Drive API service
                _driveService = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "AI_BACKUP"
                });

                _logger.LogInformation("Successfully authenticated with Google Drive");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating with Google Drive");
                return false;
            }
        }

        public async Task<string> UploadFileAsync(string localFilePath, StorageConfigModel config, string remotePath)
        {
            try
            {
                _logger.LogInformation("Uploading file to Google Drive: {LocalFilePath} -> {RemotePath}", localFilePath, remotePath);

                // Authenticate if needed
                if (_driveService == null)
                {
                    var authenticated = await AuthenticateAsync(config);
                    if (!authenticated)
                    {
                        throw new Exception("Failed to authenticate with Google Drive");
                    }
                }

                // Create file metadata
                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = Path.GetFileName(remotePath),
                    Description = $"Backup file uploaded by AI_BACKUP at {DateTime.UtcNow}"
                };

                // Set parent folder if specified
                if (!string.IsNullOrEmpty(config.FolderPath))
                {
                    fileMetadata.Parents = new List<string> { config.FolderPath };
                }

                // Create upload stream
                using var stream = new FileStream(localFilePath, FileMode.Open);

                // Create the file upload request
                var request = _driveService!.Files.Create(fileMetadata, stream, GetMimeType(localFilePath));
                request.Fields = "id, name, webViewLink";

                // Upload the file
                var response = await request.UploadAsync();
                var file = request.ResponseBody;

                if (response.Status == Google.Apis.Upload.UploadStatus.Failed)
                {
                    throw new Exception($"Google Drive upload failed: {response.Exception?.Message}");
                }

                _logger.LogInformation("File uploaded successfully to Google Drive. File ID: {FileId}", file.Id);
                return file.Id; // Return the file ID as the remote path
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file to Google Drive: {LocalFilePath}", localFilePath);
                throw;
            }
        }

        public async Task<bool> DownloadFileAsync(string remotePath, StorageConfigModel config, string localFilePath)
        {
            try
            {
                _logger.LogInformation("Downloading file from Google Drive: {RemotePath} -> {LocalFilePath}", remotePath, localFilePath);

                // Authenticate if needed
                if (_driveService == null)
                {
                    var authenticated = await AuthenticateAsync(config);
                    if (!authenticated)
                    {
                        throw new Exception("Failed to authenticate with Google Drive");
                    }
                }

                // Ensure the directory exists
                var directory = Path.GetDirectoryName(localFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Create request to get the file
                var request = _driveService!.Files.Get(remotePath);
                
                // Download the file
                using var stream = new FileStream(localFilePath, FileMode.Create);
                await request.DownloadAsync(stream);

                _logger.LogInformation("File downloaded successfully from Google Drive");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file from Google Drive: {RemotePath}", remotePath);
                return false;
            }
        }

        public async Task<bool> DeleteFileAsync(string remotePath, StorageConfigModel config)
        {
            try
            {
                _logger.LogInformation("Deleting file from Google Drive: {RemotePath}", remotePath);

                // Authenticate if needed
                if (_driveService == null)
                {
                    var authenticated = await AuthenticateAsync(config);
                    if (!authenticated)
                    {
                        throw new Exception("Failed to authenticate with Google Drive");
                    }
                }

                // Delete the file
                await _driveService!.Files.Delete(remotePath).ExecuteAsync();

                _logger.LogInformation("File deleted successfully from Google Drive");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file from Google Drive: {RemotePath}", remotePath);
                return false;
            }
        }

        public async Task<List<string>> ListFilesAsync(string remotePath, StorageConfigModel config)
        {
            try
            {
                _logger.LogInformation("Listing files from Google Drive: {RemotePath}", remotePath);

                // Authenticate if needed
                if (_driveService == null)
                {
                    var authenticated = await AuthenticateAsync(config);
                    if (!authenticated)
                    {
                        throw new Exception("Failed to authenticate with Google Drive");
                    }
                }

                var result = new List<string>();
                var request = _driveService!.Files.List();
                
                // Set query to find files in the specified folder
                if (!string.IsNullOrEmpty(remotePath))
                {
                    request.Q = $"'{remotePath}' in parents";
                }
                else if (!string.IsNullOrEmpty(config.FolderPath))
                {
                    request.Q = $"'{config.FolderPath}' in parents";
                }
                
                request.Fields = "files(id, name, mimeType)";

                // Get the files
                var response = await request.ExecuteAsync();
                
                foreach (var file in response.Files)
                {
                    result.Add(file.Id);
                }

                _logger.LogInformation("Listed {Count} files from Google Drive", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing files from Google Drive: {RemotePath}", remotePath);
                return new List<string>();
            }
        }

        public async Task<bool> TestConnectionAsync(StorageConfigModel config)
        {
            try
            {
                _logger.LogInformation("Testing connection to Google Drive");

                // Authenticate
                var authenticated = await AuthenticateAsync(config);
                if (!authenticated)
                {
                    return false;
                }

                // Try to list files to verify connection
                var request = _driveService!.Files.List();
                request.PageSize = 1;
                request.Fields = "files(id, name)";
                
                var response = await request.ExecuteAsync();
                
                _logger.LogInformation("Successfully connected to Google Drive");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connection to Google Drive");
                return false;
            }
        }

        private string GetMimeType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".txt" => "text/plain",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".zip" => "application/zip",
                ".bak" => "application/octet-stream",
                ".sql" => "application/sql",
                ".dump" => "application/octet-stream",
                ".enc" => "application/octet-stream",
                _ => "application/octet-stream"
            };
        }
    }
}