namespace AI_BACKUP.WindowsService.Services
{
    public interface ICompressionService
    {
        Task<string> CompressFileAsync(string inputFilePath, string outputFilePath);
        Task<string> CompressDirectoryAsync(string inputDirectoryPath, string outputFilePath, string searchPattern = "*.*", bool recursive = true);
        Task<string> DecompressAsync(string inputFilePath, string outputDirectoryPath);
        Task<Stream> CompressStreamAsync(Stream inputStream);
        Task<Stream> DecompressStreamAsync(Stream inputStream);
    }
}