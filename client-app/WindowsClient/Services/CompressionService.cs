using System.IO.Compression;

namespace AI_BACKUP.WindowsService.Services
{
    public class CompressionService : ICompressionService
    {
        private readonly ILogger<CompressionService> _logger;
        private readonly IFileSystemService _fileSystemService;

        public CompressionService(
            ILogger<CompressionService> logger,
            IFileSystemService fileSystemService)
        {
            _logger = logger;
            _fileSystemService = fileSystemService;
        }

        public async Task<string> CompressFileAsync(string inputFilePath, string outputFilePath)
        {
            try
            {
                // Ensure output directory exists
                var outputDir = Path.GetDirectoryName(outputFilePath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                using var outputStream = File.Create(outputFilePath);
                using var zipArchive = new ZipArchive(outputStream, ZipArchiveMode.Create);
                
                var fileName = Path.GetFileName(inputFilePath);
                var entry = zipArchive.CreateEntry(fileName, CompressionLevel.Optimal);
                
                using var entryStream = entry.Open();
                using var inputStream = await _fileSystemService.OpenFileForReadAsync(inputFilePath);
                
                await inputStream.CopyToAsync(entryStream);
                
                return outputFilePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error compressing file {InputFilePath}", inputFilePath);
                throw;
            }
        }

        public async Task<string> CompressDirectoryAsync(string inputDirectoryPath, string outputFilePath, string searchPattern = "*.*", bool recursive = true)
        {
            try
            {
                // Ensure output directory exists
                var outputDir = Path.GetDirectoryName(outputFilePath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // Get all files to compress
                var files = await _fileSystemService.GetFilesAsync(inputDirectoryPath, searchPattern, recursive);
                
                using var outputStream = File.Create(outputFilePath);
                using var zipArchive = new ZipArchive(outputStream, ZipArchiveMode.Create);
                
                foreach (var file in files)
                {
                    // Create relative path for the entry
                    var relativePath = file.Substring(inputDirectoryPath.Length).TrimStart(Path.DirectorySeparatorChar);
                    
                    // Create entry in the archive
                    var entry = zipArchive.CreateEntry(relativePath, CompressionLevel.Optimal);
                    
                    // Write file content to the entry
                    using var entryStream = entry.Open();
                    using var inputStream = await _fileSystemService.OpenFileForReadAsync(file);
                    
                    await inputStream.CopyToAsync(entryStream);
                }
                
                return outputFilePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error compressing directory {InputDirectoryPath}", inputDirectoryPath);
                throw;
            }
        }

        public async Task<string> DecompressAsync(string inputFilePath, string outputDirectoryPath)
        {
            try
            {
                // Ensure output directory exists
                if (!Directory.Exists(outputDirectoryPath))
                {
                    Directory.CreateDirectory(outputDirectoryPath);
                }

                using var zipArchive = ZipFile.OpenRead(inputFilePath);
                
                foreach (var entry in zipArchive.Entries)
                {
                    var outputPath = Path.Combine(outputDirectoryPath, entry.FullName);
                    var outputDir = Path.GetDirectoryName(outputPath);
                    
                    // Create directory if it doesn't exist
                    if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }
                    
                    // Skip if entry is a directory
                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        continue;
                    }
                    
                    // Extract file
                    using var entryStream = entry.Open();
                    using var outputStream = await _fileSystemService.OpenFileForWriteAsync(outputPath);
                    
                    await entryStream.CopyToAsync(outputStream);
                }
                
                return outputDirectoryPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decompressing file {InputFilePath}", inputFilePath);
                throw;
            }
        }

        public async Task<Stream> CompressStreamAsync(Stream inputStream)
        {
            try
            {
                var outputStream = new MemoryStream();
                
                using (var zipStream = new GZipStream(outputStream, CompressionMode.Compress, true))
                {
                    await inputStream.CopyToAsync(zipStream);
                }
                
                outputStream.Position = 0;
                return outputStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error compressing stream");
                throw;
            }
        }

        public async Task<Stream> DecompressStreamAsync(Stream inputStream)
        {
            try
            {
                var outputStream = new MemoryStream();
                
                using (var zipStream = new GZipStream(inputStream, CompressionMode.Decompress, true))
                {
                    await zipStream.CopyToAsync(outputStream);
                }
                
                outputStream.Position = 0;
                return outputStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decompressing stream");
                throw;
            }
        }
    }
}