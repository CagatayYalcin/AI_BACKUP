using AI_BACKUP.WindowsService.Models;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using System.Net.Http.Headers;

namespace AI_BACKUP.WindowsService.Services
{
    public class OneDriveService : ICloudStorageService
    {
        private readonly ILogger<OneDriveService> _logger;
        private readonly ServiceSettings _settings;
        private readonly IFileSystemService _fileSystemService;
        private GraphServiceClient? _graphClient;

        public OneDriveService(
            ILogger<OneDriveService> logger,
            IOptions<ServiceSettings> settings,
            IFileSystemService fileSystemService)
        {
            _logger = logger;
            _settings = settings.Value;
            _fileSystemService = fileSystemService;
        }

        public StorageType StorageType => StorageType.OneDrive;

        public async Task<bool> AuthenticateAsync(StorageConfigModel config)
        {
            try
            {
                _logger.LogInformation("Authenticating with OneDrive");

                // Check if we have client secrets
                if (string.IsNullOrEmpty(config.AccessKey) || string.IsNullOrEmpty(config.SecretKey))
                {
                    throw new ArgumentException("OneDrive client ID and client secret are required");
                }

                // Create credential directory if it doesn't exist
                var credentialDir = Path.Combine(_settings.TempDirectory, "OneDriveCredentials");
                if (!Directory.Exists(credentialDir))
                {
                    Directory.CreateDirectory(credentialDir);
                }

                // Create the MSAL confidential client application
                var app = ConfidentialClientApplicationBuilder
                    .Create(config.AccessKey)
                    .WithClientSecret(config.SecretKey)
                    .WithRedirectUri("http://localhost")
                    .WithAuthority(AzureCloudInstance.AzurePublic, "common")
                    .Build();

                // Define the scopes
                string[] scopes = new string[] { "Files.ReadWrite.All" };

                // Create the client credentials
                var authProvider = new ClientCredentialProvider(app);

                // Create the Graph client
                _graphClient = new GraphServiceClient(authProvider);

                // Test the connection
                await _graphClient.Me.Drive.Root.Request().GetAsync();

                _logger.LogInformation("Successfully authenticated with OneDrive");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating with OneDrive");
                return false;
            }
        }

        public async Task<string> UploadFileAsync(string localFilePath, StorageConfigModel config, string remotePath)
        {
            try
            {
                _logger.LogInformation("Uploading file to OneDrive: {LocalFilePath} -> {RemotePath}", localFilePath, remotePath);

                // Authenticate if needed
                if (_graphClient == null)
                {
                    var authenticated = await AuthenticateAsync(config);
                    if (!authenticated)
                    {
                        throw new Exception("Failed to authenticate with OneDrive");
                    }
                }

                // Get file info
                var fileInfo = new FileInfo(localFilePath);
                var fileName = Path.GetFileName(remotePath);
                var folderPath = Path.GetDirectoryName(remotePath)?.Replace('\\', '/') ?? "";

                // Create folder path if it doesn't exist
                var driveItem = await EnsureFolderPathExistsAsync(folderPath, config);
                var folderId = driveItem.Id;

                // Upload the file
                using var stream = new FileStream(localFilePath, FileMode.Open);
                
                // For small files (less than 4MB), use simple upload
                if (fileInfo.Length < 4 * 1024 * 1024)
                {
                    var uploadedItem = await _graphClient!.Me.Drive.Items[folderId].ItemWithPath(fileName)
                        .Content
                        .Request()
                        .PutAsync<DriveItem>(stream);

                    _logger.LogInformation("File uploaded successfully to OneDrive. File ID: {FileId}", uploadedItem.Id);
                    return uploadedItem.Id;
                }
                // For larger files, use resumable upload
                else
                {
                    // Create upload session
                    var uploadProps = new DriveItemUploadableProperties
                    {
                        ODataType = null,
                        AdditionalData = new Dictionary<string, object>
                        {
                            { "@microsoft.graph.conflictBehavior", "replace" }
                        }
                    };

                    var uploadSession = await _graphClient!.Me.Drive.Items[folderId].ItemWithPath(fileName)
                        .CreateUploadSession(uploadProps)
                        .Request()
                        .PostAsync();

                    // Create upload task
                    var maxSliceSize = 320 * 1024; // 320 KB
                    var fileUploadTask = new LargeFileUploadTask<DriveItem>(uploadSession, stream, maxSliceSize);

                    // Upload the file
                    var uploadResult = await fileUploadTask.UploadAsync();

                    if (uploadResult.UploadSucceeded)
                    {
                        _logger.LogInformation("Large file uploaded successfully to OneDrive. File ID: {FileId}", uploadResult.ItemResponse.Id);
                        return uploadResult.ItemResponse.Id;
                    }
                    else
                    {
                        throw new Exception("Failed to upload large file to OneDrive");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file to OneDrive: {LocalFilePath}", localFilePath);
                throw;
            }
        }

        public async Task<bool> DownloadFileAsync(string remotePath, StorageConfigModel config, string localFilePath)
        {
            try
            {
                _logger.LogInformation("Downloading file from OneDrive: {RemotePath} -> {LocalFilePath}", remotePath, localFilePath);

                // Authenticate if needed
                if (_graphClient == null)
                {
                    var authenticated = await AuthenticateAsync(config);
                    if (!authenticated)
                    {
                        throw new Exception("Failed to authenticate with OneDrive");
                    }
                }

                // Ensure the directory exists
                var directory = Path.GetDirectoryName(localFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Download the file
                using var stream = await _graphClient!.Me.Drive.Items[remotePath].Content.Request().GetAsync();
                using var fileStream = new FileStream(localFilePath, FileMode.Create);
                await stream.CopyToAsync(fileStream);

                _logger.LogInformation("File downloaded successfully from OneDrive");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file from OneDrive: {RemotePath}", remotePath);
                return false;
            }
        }

        public async Task<bool> DeleteFileAsync(string remotePath, StorageConfigModel config)
        {
            try
            {
                _logger.LogInformation("Deleting file from OneDrive: {RemotePath}", remotePath);

                // Authenticate if needed
                if (_graphClient == null)
                {
                    var authenticated = await AuthenticateAsync(config);
                    if (!authenticated)
                    {
                        throw new Exception("Failed to authenticate with OneDrive");
                    }
                }

                // Delete the file
                await _graphClient!.Me.Drive.Items[remotePath].Request().DeleteAsync();

                _logger.LogInformation("File deleted successfully from OneDrive");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file from OneDrive: {RemotePath}", remotePath);
                return false;
            }
        }

        public async Task<List<string>> ListFilesAsync(string remotePath, StorageConfigModel config)
        {
            try
            {
                _logger.LogInformation("Listing files from OneDrive: {RemotePath}", remotePath);

                // Authenticate if needed
                if (_graphClient == null)
                {
                    var authenticated = await AuthenticateAsync(config);
                    if (!authenticated)
                    {
                        throw new Exception("Failed to authenticate with OneDrive");
                    }
                }

                var result = new List<string>();
                
                // Get the folder ID
                var folderId = !string.IsNullOrEmpty(remotePath) ? remotePath : "root";
                
                // List files in the folder
                var children = await _graphClient!.Me.Drive.Items[folderId].Children.Request().GetAsync();
                
                foreach (var child in children)
                {
                    if (child.File != null) // Only include files, not folders
                    {
                        result.Add(child.Id);
                    }
                }

                _logger.LogInformation("Listed {Count} files from OneDrive", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing files from OneDrive: {RemotePath}", remotePath);
                return new List<string>();
            }
        }

        public async Task<bool> TestConnectionAsync(StorageConfigModel config)
        {
            try
            {
                _logger.LogInformation("Testing connection to OneDrive");

                // Authenticate
                var authenticated = await AuthenticateAsync(config);
                if (!authenticated)
                {
                    return false;
                }

                // Try to get drive info to verify connection
                var drive = await _graphClient!.Me.Drive.Request().GetAsync();
                
                _logger.LogInformation("Successfully connected to OneDrive");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connection to OneDrive");
                return false;
            }
        }

        private async Task<DriveItem> EnsureFolderPathExistsAsync(string folderPath, StorageConfigModel config)
        {
            // If no folder path is specified, return the root folder
            if (string.IsNullOrEmpty(folderPath))
            {
                return await _graphClient!.Me.Drive.Root.Request().GetAsync();
            }

            // Split the path into segments
            var segments = folderPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            var currentItem = await _graphClient!.Me.Drive.Root.Request().GetAsync();

            // Create each folder in the path if it doesn't exist
            foreach (var segment in segments)
            {
                try
                {
                    // Try to get the folder
                    var folder = await _graphClient.Me.Drive.Items[currentItem.Id].ItemWithPath(segment).Request().GetAsync();
                    currentItem = folder;
                }
                catch (ServiceException)
                {
                    // Folder doesn't exist, create it
                    var folderToCreate = new DriveItem
                    {
                        Name = segment,
                        Folder = new Folder(),
                        AdditionalData = new Dictionary<string, object>
                        {
                            { "@microsoft.graph.conflictBehavior", "rename" }
                        }
                    };

                    currentItem = await _graphClient.Me.Drive.Items[currentItem.Id].Children.Request().AddAsync(folderToCreate);
                }
            }

            return currentItem;
        }
    }
}