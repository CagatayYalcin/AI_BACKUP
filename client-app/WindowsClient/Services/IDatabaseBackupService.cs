using AI_BACKUP.WindowsService.Models;

namespace AI_BACKUP.WindowsService.Services
{
    public interface IDatabaseBackupService
    {
        Task<string> BackupMsSqlDatabaseAsync(DatabaseSettings settings, string outputPath);
        Task<string> BackupMySqlDatabaseAsync(DatabaseSettings settings, string outputPath);
        Task<string> BackupPostgreSqlDatabaseAsync(DatabaseSettings settings, string outputPath);
        Task<string> BackupMongoDbDatabaseAsync(DatabaseSettings settings, string outputPath);
        Task<bool> RestoreMsSqlDatabaseAsync(string backupFilePath, DatabaseSettings settings);
        Task<bool> RestoreMySqlDatabaseAsync(string backupFilePath, DatabaseSettings settings);
        Task<bool> RestorePostgreSqlDatabaseAsync(string backupFilePath, DatabaseSettings settings);
        Task<bool> RestoreMongoDbDatabaseAsync(string backupFilePath, DatabaseSettings settings);
        Task<bool> TestConnectionAsync(BackupType databaseType, DatabaseSettings settings);
    }
}