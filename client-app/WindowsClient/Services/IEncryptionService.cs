namespace AI_BACKUP.WindowsService.Services
{
    public interface IEncryptionService
    {
        Task<Stream> EncryptStreamAsync(Stream inputStream, string password);
        Task<Stream> DecryptStreamAsync(Stream inputStream, string password);
        Task<string> EncryptFileAsync(string inputFilePath, string outputFilePath, string password);
        Task<string> DecryptFileAsync(string inputFilePath, string outputFilePath, string password);
        string GenerateEncryptionKey();
        string EncryptText(string plainText, string password);
        string DecryptText(string encryptedText, string password);
    }
}