using AI_BACKUP.WindowsService.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DeltaCompressionDotNet;
using DeltaCompressionDotNet.MsDelta;

namespace AI_BACKUP.WindowsService.Services
{
    /// <summary>
    /// Service for handling delta compression of files
    /// </summary>
    public class DeltaCompressionService : IDeltaCompressionService
    {
        private readonly ILogger<DeltaCompressionService> _logger;
        private readonly IFileSystemService _fileSystemService;

        public DeltaCompressionService(
            ILogger<DeltaCompressionService> logger,
            IFileSystemService fileSystemService)
        {
            _logger = logger;
            _fileSystemService = fileSystemService;
        }

        /// <summary>
        /// Creates a delta file containing only the changes between the source and target files
        /// </summary>
        /// <param name="sourcePath">Path to the source file (previous version)</param>
        /// <param name="targetPath">Path to the target file (current version)</param>
        /// <param name="deltaPath">Path where the delta file will be saved</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> CreateDeltaAsync(string sourcePath, string targetPath, string deltaPath)
        {
            try
            {
                _logger.LogInformation("Creating delta between {SourcePath} and {TargetPath}", sourcePath, targetPath);

                // Ensure the directory exists
                var deltaDirectory = Path.GetDirectoryName(deltaPath);
                if (!string.IsNullOrEmpty(deltaDirectory) && !Directory.Exists(deltaDirectory))
                {
                    Directory.CreateDirectory(deltaDirectory);
                }

                // Create delta using MSDelta
                var deltaCompressor = new MsDeltaCompression();
                deltaCompressor.CreateDelta(sourcePath, targetPath, deltaPath);

                _logger.LogInformation("Delta created successfully at {DeltaPath}", deltaPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating delta between {SourcePath} and {TargetPath}", sourcePath, targetPath);
                return false;
            }
        }

        /// <summary>
        /// Applies a delta file to a source file to create the target file
        /// </summary>
        /// <param name="sourcePath">Path to the source file (previous version)</param>
        /// <param name="deltaPath">Path to the delta file</param>
        /// <param name="targetPath">Path where the reconstructed target file will be saved</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> ApplyDeltaAsync(string sourcePath, string deltaPath, string targetPath)
        {
            try
            {
                _logger.LogInformation("Applying delta {DeltaPath} to {SourcePath}", deltaPath, sourcePath);

                // Ensure the directory exists
                var targetDirectory = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(targetDirectory) && !Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                // Apply delta using MSDelta
                var deltaCompressor = new MsDeltaCompression();
                deltaCompressor.ApplyDelta(deltaPath, sourcePath, targetPath);

                _logger.LogInformation("Delta applied successfully, created {TargetPath}", targetPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying delta {DeltaPath} to {SourcePath}", deltaPath, sourcePath);
                return false;
            }
        }

        /// <summary>
        /// Calculates the SHA-256 hash of a file
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>The hash as a hexadecimal string</returns>
        public async Task<string> CalculateFileHashAsync(string filePath)
        {
            try
            {
                _logger.LogDebug("Calculating hash for file {FilePath}", filePath);

                using (var stream = File.OpenRead(filePath))
                using (var sha256 = SHA256.Create())
                {
                    var hashBytes = await sha256.ComputeHashAsync(stream);
                    var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                    
                    _logger.LogDebug("Hash for file {FilePath}: {Hash}", filePath, hash);
                    return hash;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating hash for file {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Determines if a file has changed by comparing its hash with a previous hash
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <param name="previousHash">Previous hash to compare against</param>
        /// <returns>True if the file has changed, false otherwise</returns>
        public async Task<bool> HasFileChangedAsync(string filePath, string previousHash)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("File {FilePath} does not exist", filePath);
                    return true; // Consider as changed if file doesn't exist
                }

                var currentHash = await CalculateFileHashAsync(filePath);
                var hasChanged = !string.Equals(currentHash, previousHash, StringComparison.OrdinalIgnoreCase);
                
                _logger.LogDebug("File {FilePath} has changed: {HasChanged}", filePath, hasChanged);
                return hasChanged;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if file {FilePath} has changed", filePath);
                return true; // Consider as changed if there's an error
            }
        }
    }

    /// <summary>
    /// Interface for delta compression service
    /// </summary>
    public interface IDeltaCompressionService
    {
        Task<bool> CreateDeltaAsync(string sourcePath, string targetPath, string deltaPath);
        Task<bool> ApplyDeltaAsync(string sourcePath, string deltaPath, string targetPath);
        Task<string> CalculateFileHashAsync(string filePath);
        Task<bool> HasFileChangedAsync(string filePath, string previousHash);
    }
}