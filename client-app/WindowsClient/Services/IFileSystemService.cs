namespace AI_BACKUP.WindowsService.Services
{
    public interface IFileSystemService
    {
        Task<List<string>> GetFilesAsync(string path, string searchPattern, bool recursive);
        Task<long> GetFileSizeAsync(string filePath);
        Task<DateTime> GetLastModifiedTimeAsync(string filePath);
        Task<string> CalculateChecksumAsync(string filePath);
        Task<bool> FileExistsAsync(string filePath);
        Task<bool> DirectoryExistsAsync(string directoryPath);
        Task CreateDirectoryAsync(string directoryPath);
        Task<string> CreateTempDirectoryAsync();
        Task DeleteDirectoryAsync(string directoryPath, bool recursive);
        Task DeleteFileAsync(string filePath);
        Task CopyFileAsync(string sourcePath, string destinationPath, bool overwrite);
        Task MoveFileAsync(string sourcePath, string destinationPath, bool overwrite);
        Task<Stream> OpenFileForReadAsync(string filePath);
        Task<Stream> OpenFileForWriteAsync(string filePath);
        Task<string> GetTempFilePathAsync(string extension);
    }
}