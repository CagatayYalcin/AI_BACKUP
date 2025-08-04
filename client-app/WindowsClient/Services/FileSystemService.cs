using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace AI_BACKUP.WindowsService.Services
{
    public class FileSystemService : IFileSystemService
    {
        private readonly ILogger<FileSystemService> _logger;
        private readonly ServiceSettings _settings;

        public FileSystemService(
            ILogger<FileSystemService> logger,
            IOptions<ServiceSettings> settings)
        {
            _logger = logger;
            _settings = settings.Value;
            
            // Ensure temp directory exists
            if (!string.IsNullOrEmpty(_settings.TempDirectory) && !Directory.Exists(_settings.TempDirectory))
            {
                Directory.CreateDirectory(_settings.TempDirectory);
            }
        }

        public async Task<List<string>> GetFilesAsync(string path, string searchPattern, bool recursive)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                    return Directory.GetFiles(path, searchPattern, searchOption).ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting files from {Path} with pattern {Pattern}", path, searchPattern);
                    return new List<string>();
                }
            });
        }

        public async Task<long> GetFileSizeAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var fileInfo = new FileInfo(filePath);
                    return fileInfo.Length;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting file size for {FilePath}", filePath);
                    return 0;
                }
            });
        }

        public async Task<DateTime> GetLastModifiedTimeAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var fileInfo = new FileInfo(filePath);
                    return fileInfo.LastWriteTimeUtc;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting last modified time for {FilePath}", filePath);
                    return DateTime.MinValue;
                }
            });
        }

        public async Task<string> CalculateChecksumAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var md5 = MD5.Create();
                    using var stream = File.OpenRead(filePath);
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating checksum for {FilePath}", filePath);
                    return string.Empty;
                }
            });
        }

        public async Task<bool> FileExistsAsync(string filePath)
        {
            return await Task.Run(() => File.Exists(filePath));
        }

        public async Task<bool> DirectoryExistsAsync(string directoryPath)
        {
            return await Task.Run(() => Directory.Exists(directoryPath));
        }

        public async Task CreateDirectoryAsync(string directoryPath)
        {
            await Task.Run(() =>
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
            });
        }

        public async Task<string> CreateTempDirectoryAsync()
        {
            return await Task.Run(() =>
            {
                var tempPath = Path.Combine(
                    string.IsNullOrEmpty(_settings.TempDirectory) ? Path.GetTempPath() : _settings.TempDirectory,
                    $"AI_BACKUP_{Guid.NewGuid():N}");
                
                Directory.CreateDirectory(tempPath);
                return tempPath;
            });
        }

        public async Task DeleteDirectoryAsync(string directoryPath, bool recursive)
        {
            await Task.Run(() =>
            {
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, recursive);
                }
            });
        }

        public async Task DeleteFileAsync(string filePath)
        {
            await Task.Run(() =>
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            });
        }

        public async Task CopyFileAsync(string sourcePath, string destinationPath, bool overwrite)
        {
            await Task.Run(() =>
            {
                var destinationDir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }
                
                File.Copy(sourcePath, destinationPath, overwrite);
            });
        }

        public async Task MoveFileAsync(string sourcePath, string destinationPath, bool overwrite)
        {
            await Task.Run(() =>
            {
                var destinationDir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }
                
                if (overwrite && File.Exists(destinationPath))
                {
                    File.Delete(destinationPath);
                }
                
                File.Move(sourcePath, destinationPath);
            });
        }

        public async Task<Stream> OpenFileForReadAsync(string filePath)
        {
            return await Task.Run(() => File.OpenRead(filePath) as Stream);
        }

        public async Task<Stream> OpenFileForWriteAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                var directoryPath = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                
                return File.Create(filePath) as Stream;
            });
        }

        public async Task<string> GetTempFilePathAsync(string extension)
        {
            return await Task.Run(() =>
            {
                var tempDir = string.IsNullOrEmpty(_settings.TempDirectory) ? Path.GetTempPath() : _settings.TempDirectory;
                return Path.Combine(tempDir, $"AI_BACKUP_{Guid.NewGuid():N}{extension}");
            });
        }
    }
}