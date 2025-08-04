using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace AI_BACKUP.WindowsService.Services
{
    public class EncryptionService : IEncryptionService
    {
        private readonly ILogger<EncryptionService> _logger;
        private readonly ServiceSettings _settings;
        private readonly IFileSystemService _fileSystemService;

        public EncryptionService(
            ILogger<EncryptionService> logger,
            IOptions<ServiceSettings> settings,
            IFileSystemService fileSystemService)
        {
            _logger = logger;
            _settings = settings.Value;
            _fileSystemService = fileSystemService;
        }

        public async Task<Stream> EncryptStreamAsync(Stream inputStream, string password)
        {
            try
            {
                // Create output memory stream
                var outputStream = new MemoryStream();
                
                // Generate random salt
                byte[] salt = GenerateRandomBytes(16);
                
                // Write salt to the beginning of the output stream
                await outputStream.WriteAsync(salt, 0, salt.Length);
                
                // Generate key and IV from password and salt
                using var key = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
                byte[] keyBytes = key.GetBytes(32); // 256 bits
                byte[] ivBytes = key.GetBytes(16);  // 128 bits
                
                // Create AES encryptor
                using var aes = Aes.Create();
                aes.Key = keyBytes;
                aes.IV = ivBytes;
                aes.Mode = CipherMode.CBC;
                
                // Create crypto stream
                using var cryptoStream = new CryptoStream(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
                
                // Copy input to crypto stream
                await inputStream.CopyToAsync(cryptoStream);
                await cryptoStream.FlushFinalBlockAsync();
                
                // Reset position to beginning
                outputStream.Position = 0;
                
                return outputStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encrypting stream");
                throw;
            }
        }

        public async Task<Stream> DecryptStreamAsync(Stream inputStream, string password)
        {
            try
            {
                // Create output memory stream
                var outputStream = new MemoryStream();
                
                // Read salt from the beginning of the input stream
                byte[] salt = new byte[16];
                await inputStream.ReadAsync(salt, 0, salt.Length);
                
                // Generate key and IV from password and salt
                using var key = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
                byte[] keyBytes = key.GetBytes(32); // 256 bits
                byte[] ivBytes = key.GetBytes(16);  // 128 bits
                
                // Create AES decryptor
                using var aes = Aes.Create();
                aes.Key = keyBytes;
                aes.IV = ivBytes;
                aes.Mode = CipherMode.CBC;
                
                // Create crypto stream
                using var cryptoStream = new CryptoStream(inputStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
                
                // Copy crypto stream to output
                await cryptoStream.CopyToAsync(outputStream);
                
                // Reset position to beginning
                outputStream.Position = 0;
                
                return outputStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting stream");
                throw;
            }
        }

        public async Task<string> EncryptFileAsync(string inputFilePath, string outputFilePath, string password)
        {
            try
            {
                using var inputStream = await _fileSystemService.OpenFileForReadAsync(inputFilePath);
                using var encryptedStream = await EncryptStreamAsync(inputStream, password);
                using var outputStream = await _fileSystemService.OpenFileForWriteAsync(outputFilePath);
                
                await encryptedStream.CopyToAsync(outputStream);
                
                return outputFilePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encrypting file {InputFilePath}", inputFilePath);
                throw;
            }
        }

        public async Task<string> DecryptFileAsync(string inputFilePath, string outputFilePath, string password)
        {
            try
            {
                using var inputStream = await _fileSystemService.OpenFileForReadAsync(inputFilePath);
                using var decryptedStream = await DecryptStreamAsync(inputStream, password);
                using var outputStream = await _fileSystemService.OpenFileForWriteAsync(outputFilePath);
                
                await decryptedStream.CopyToAsync(outputStream);
                
                return outputFilePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting file {InputFilePath}", inputFilePath);
                throw;
            }
        }

        public string GenerateEncryptionKey()
        {
            byte[] keyBytes = GenerateRandomBytes(32);
            return Convert.ToBase64String(keyBytes);
        }

        public string EncryptText(string plainText, string password)
        {
            try
            {
                // Generate random salt
                byte[] salt = GenerateRandomBytes(16);
                
                // Generate key and IV from password and salt
                using var key = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
                byte[] keyBytes = key.GetBytes(32); // 256 bits
                byte[] ivBytes = key.GetBytes(16);  // 128 bits
                
                // Create AES encryptor
                using var aes = Aes.Create();
                aes.Key = keyBytes;
                aes.IV = ivBytes;
                aes.Mode = CipherMode.CBC;
                
                // Encrypt the text
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                using var encryptor = aes.CreateEncryptor();
                byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                
                // Combine salt and encrypted bytes
                byte[] result = new byte[salt.Length + encryptedBytes.Length];
                Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
                Buffer.BlockCopy(encryptedBytes, 0, result, salt.Length, encryptedBytes.Length);
                
                // Return as base64 string
                return Convert.ToBase64String(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encrypting text");
                throw;
            }
        }

        public string DecryptText(string encryptedText, string password)
        {
            try
            {
                // Decode base64 string
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                
                // Extract salt
                byte[] salt = new byte[16];
                Buffer.BlockCopy(encryptedBytes, 0, salt, 0, salt.Length);
                
                // Generate key and IV from password and salt
                using var key = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
                byte[] keyBytes = key.GetBytes(32); // 256 bits
                byte[] ivBytes = key.GetBytes(16);  // 128 bits
                
                // Create AES decryptor
                using var aes = Aes.Create();
                aes.Key = keyBytes;
                aes.IV = ivBytes;
                aes.Mode = CipherMode.CBC;
                
                // Extract encrypted data
                int encryptedDataLength = encryptedBytes.Length - salt.Length;
                byte[] encryptedData = new byte[encryptedDataLength];
                Buffer.BlockCopy(encryptedBytes, salt.Length, encryptedData, 0, encryptedDataLength);
                
                // Decrypt the data
                using var decryptor = aes.CreateDecryptor();
                byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
                
                // Return as string
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting text");
                throw;
            }
        }

        private byte[] GenerateRandomBytes(int length)
        {
            var randomBytes = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return randomBytes;
        }
    }
}