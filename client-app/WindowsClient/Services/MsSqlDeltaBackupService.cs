using AI_BACKUP.WindowsService.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;

namespace AI_BACKUP.WindowsService.Services
{
    /// <summary>
    /// Service for handling MSSQL database backups with differential and transaction log backups
    /// </summary>
    public class MsSqlDeltaBackupService : IMsSqlDeltaBackupService
    {
        private readonly ILogger<MsSqlDeltaBackupService> _logger;
        private readonly IFileSystemService _fileSystemService;

        public MsSqlDeltaBackupService(
            ILogger<MsSqlDeltaBackupService> logger,
            IFileSystemService fileSystemService)
        {
            _logger = logger;
            _fileSystemService = fileSystemService;
        }

        /// <summary>
        /// Performs a full backup of the specified database
        /// </summary>
        /// <param name="settings">Database connection settings</param>
        /// <param name="outputPath">Path where the backup file will be saved</param>
        /// <returns>Path to the backup file</returns>
        public async Task<string> PerformFullBackupAsync(DatabaseSettings settings, string outputPath)
        {
            try
            {
                _logger.LogInformation("Performing full backup of database {DatabaseName} to {OutputPath}", 
                    settings.DatabaseName, outputPath);

                // Ensure the directory exists
                var directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory) && !await _fileSystemService.DirectoryExistsAsync(directory))
                {
                    await _fileSystemService.CreateDirectoryAsync(directory);
                }

                using (var connection = new SqlConnection(GetConnectionString(settings)))
                {
                    await connection.OpenAsync();

                    var backupCommand = new SqlCommand(
                        $"BACKUP DATABASE [{settings.DatabaseName}] TO DISK = '{outputPath}' WITH INIT, FORMAT", 
                        connection);

                    await backupCommand.ExecuteNonQueryAsync();
                }

                _logger.LogInformation("Full backup of database {DatabaseName} completed successfully", 
                    settings.DatabaseName);

                return outputPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing full backup of database {DatabaseName}", 
                    settings.DatabaseName);
                throw;
            }
        }

        /// <summary>
        /// Performs a differential backup of the specified database
        /// </summary>
        /// <param name="settings">Database connection settings</param>
        /// <param name="outputPath">Path where the backup file will be saved</param>
        /// <returns>Path to the backup file</returns>
        public async Task<string> PerformDifferentialBackupAsync(DatabaseSettings settings, string outputPath)
        {
            try
            {
                _logger.LogInformation("Performing differential backup of database {DatabaseName} to {OutputPath}", 
                    settings.DatabaseName, outputPath);

                // Ensure the directory exists
                var directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory) && !await _fileSystemService.DirectoryExistsAsync(directory))
                {
                    await _fileSystemService.CreateDirectoryAsync(directory);
                }

                using (var connection = new SqlConnection(GetConnectionString(settings)))
                {
                    await connection.OpenAsync();

                    var backupCommand = new SqlCommand(
                        $"BACKUP DATABASE [{settings.DatabaseName}] TO DISK = '{outputPath}' WITH DIFFERENTIAL, INIT", 
                        connection);

                    await backupCommand.ExecuteNonQueryAsync();
                }

                _logger.LogInformation("Differential backup of database {DatabaseName} completed successfully", 
                    settings.DatabaseName);

                return outputPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing differential backup of database {DatabaseName}", 
                    settings.DatabaseName);
                throw;
            }
        }

        /// <summary>
        /// Performs a transaction log backup of the specified database
        /// </summary>
        /// <param name="settings">Database connection settings</param>
        /// <param name="outputPath">Path where the backup file will be saved</param>
        /// <returns>Path to the backup file</returns>
        public async Task<string> PerformTransactionLogBackupAsync(DatabaseSettings settings, string outputPath)
        {
            try
            {
                _logger.LogInformation("Performing transaction log backup of database {DatabaseName} to {OutputPath}", 
                    settings.DatabaseName, outputPath);

                // Ensure the directory exists
                var directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory) && !await _fileSystemService.DirectoryExistsAsync(directory))
                {
                    await _fileSystemService.CreateDirectoryAsync(directory);
                }

                using (var connection = new SqlConnection(GetConnectionString(settings)))
                {
                    await connection.OpenAsync();

                    // First check if the database is in FULL or BULK_LOGGED recovery model
                    var recoveryModelCommand = new SqlCommand(
                        $"SELECT recovery_model_desc FROM sys.databases WHERE name = '{settings.DatabaseName}'", 
                        connection);

                    var recoveryModel = (string)await recoveryModelCommand.ExecuteScalarAsync();

                    if (recoveryModel != "FULL" && recoveryModel != "BULK_LOGGED")
                    {
                        _logger.LogWarning("Database {DatabaseName} is in {RecoveryModel} recovery model. " +
                            "Transaction log backup requires FULL or BULK_LOGGED recovery model.", 
                            settings.DatabaseName, recoveryModel);

                        // Change recovery model to FULL
                        var changeRecoveryModelCommand = new SqlCommand(
                            $"ALTER DATABASE [{settings.DatabaseName}] SET RECOVERY FULL", 
                            connection);

                        await changeRecoveryModelCommand.ExecuteNonQueryAsync();

                        _logger.LogInformation("Changed recovery model of database {DatabaseName} to FULL", 
                            settings.DatabaseName);
                    }

                    var backupCommand = new SqlCommand(
                        $"BACKUP LOG [{settings.DatabaseName}] TO DISK = '{outputPath}' WITH INIT", 
                        connection);

                    await backupCommand.ExecuteNonQueryAsync();
                }

                _logger.LogInformation("Transaction log backup of database {DatabaseName} completed successfully", 
                    settings.DatabaseName);

                return outputPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing transaction log backup of database {DatabaseName}", 
                    settings.DatabaseName);
                throw;
            }
        }

        /// <summary>
        /// Restores a database from a full backup
        /// </summary>
        /// <param name="settings">Database connection settings</param>
        /// <param name="backupPath">Path to the backup file</param>
        /// <param name="newDatabaseName">Optional new name for the restored database</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> RestoreFullBackupAsync(DatabaseSettings settings, string backupPath, string newDatabaseName = null)
        {
            try
            {
                var targetDbName = newDatabaseName ?? settings.DatabaseName;
                
                _logger.LogInformation("Restoring database {TargetDbName} from full backup {BackupPath}", 
                    targetDbName, backupPath);

                using (var connection = new SqlConnection(GetMasterConnectionString(settings)))
                {
                    await connection.OpenAsync();

                    // Check if database exists
                    var checkDbCommand = new SqlCommand(
                        $"SELECT COUNT(*) FROM sys.databases WHERE name = '{targetDbName}'", 
                        connection);

                    var dbExists = (int)await checkDbCommand.ExecuteScalarAsync() > 0;

                    // If restoring to existing database, set it to single user mode
                    if (dbExists)
                    {
                        var singleUserCommand = new SqlCommand(
                            $"ALTER DATABASE [{targetDbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE", 
                            connection);

                        await singleUserCommand.ExecuteNonQueryAsync();
                    }

                    // Get logical file names from backup
                    var fileListCommand = new SqlCommand(
                        $"RESTORE FILELISTONLY FROM DISK = '{backupPath}'", 
                        connection);

                    var fileListReader = await fileListCommand.ExecuteReaderAsync();
                    
                    var dataFileLogicalName = "";
                    var logFileLogicalName = "";
                    
                    while (await fileListReader.ReadAsync())
                    {
                        var type = fileListReader["Type"].ToString();
                        var logicalName = fileListReader["LogicalName"].ToString();
                        
                        if (type == "D") // Data file
                        {
                            dataFileLogicalName = logicalName;
                        }
                        else if (type == "L") // Log file
                        {
                            logFileLogicalName = logicalName;
                        }
                    }
                    
                    fileListReader.Close();

                    // Get default data and log file paths
                    var defaultPathsCommand = new SqlCommand(
                        "SELECT SERVERPROPERTY('InstanceDefaultDataPath') AS DataPath, " +
                        "SERVERPROPERTY('InstanceDefaultLogPath') AS LogPath", 
                        connection);

                    var pathsReader = await defaultPathsCommand.ExecuteReaderAsync();
                    await pathsReader.ReadAsync();
                    
                    var dataPath = pathsReader["DataPath"].ToString();
                    var logPath = pathsReader["LogPath"].ToString();
                    
                    pathsReader.Close();

                    // Build restore command
                    var restoreCommand = new SqlCommand(
                        $"RESTORE DATABASE [{targetDbName}] FROM DISK = '{backupPath}' WITH REPLACE, " +
                        $"MOVE '{dataFileLogicalName}' TO '{Path.Combine(dataPath, targetDbName + ".mdf")}', " +
                        $"MOVE '{logFileLogicalName}' TO '{Path.Combine(logPath, targetDbName + "_log.ldf")}'", 
                        connection);

                    await restoreCommand.ExecuteNonQueryAsync();

                    // Set database back to multi user mode
                    var multiUserCommand = new SqlCommand(
                        $"ALTER DATABASE [{targetDbName}] SET MULTI_USER", 
                        connection);

                    await multiUserCommand.ExecuteNonQueryAsync();
                }

                _logger.LogInformation("Database {TargetDbName} restored successfully from full backup", 
                    targetDbName);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring database {TargetDbName} from full backup", 
                    newDatabaseName ?? settings.DatabaseName);
                return false;
            }
        }

        /// <summary>
        /// Restores a database from a full backup and applies differential and transaction log backups
        /// </summary>
        /// <param name="settings">Database connection settings</param>
        /// <param name="fullBackupPath">Path to the full backup file</param>
        /// <param name="differentialBackupPath">Path to the differential backup file (optional)</param>
        /// <param name="transactionLogBackupPaths">Paths to the transaction log backup files (optional)</param>
        /// <param name="newDatabaseName">Optional new name for the restored database</param>
        /// <param name="restoreToPointInTime">Optional point in time to restore to</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> RestoreWithDeltaBackupsAsync(
            DatabaseSettings settings, 
            string fullBackupPath, 
            string differentialBackupPath = null, 
            string[] transactionLogBackupPaths = null, 
            string newDatabaseName = null,
            DateTime? restoreToPointInTime = null)
        {
            try
            {
                var targetDbName = newDatabaseName ?? settings.DatabaseName;
                
                _logger.LogInformation("Restoring database {TargetDbName} with delta backups", targetDbName);

                using (var connection = new SqlConnection(GetMasterConnectionString(settings)))
                {
                    await connection.OpenAsync();

                    // Check if database exists
                    var checkDbCommand = new SqlCommand(
                        $"SELECT COUNT(*) FROM sys.databases WHERE name = '{targetDbName}'", 
                        connection);

                    var dbExists = (int)await checkDbCommand.ExecuteScalarAsync() > 0;

                    // If restoring to existing database, set it to single user mode
                    if (dbExists)
                    {
                        var singleUserCommand = new SqlCommand(
                            $"ALTER DATABASE [{targetDbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE", 
                            connection);

                        await singleUserCommand.ExecuteNonQueryAsync();
                    }

                    // Get logical file names from backup
                    var fileListCommand = new SqlCommand(
                        $"RESTORE FILELISTONLY FROM DISK = '{fullBackupPath}'", 
                        connection);

                    var fileListReader = await fileListCommand.ExecuteReaderAsync();
                    
                    var dataFileLogicalName = "";
                    var logFileLogicalName = "";
                    
                    while (await fileListReader.ReadAsync())
                    {
                        var type = fileListReader["Type"].ToString();
                        var logicalName = fileListReader["LogicalName"].ToString();
                        
                        if (type == "D") // Data file
                        {
                            dataFileLogicalName = logicalName;
                        }
                        else if (type == "L") // Log file
                        {
                            logFileLogicalName = logicalName;
                        }
                    }
                    
                    fileListReader.Close();

                    // Get default data and log file paths
                    var defaultPathsCommand = new SqlCommand(
                        "SELECT SERVERPROPERTY('InstanceDefaultDataPath') AS DataPath, " +
                        "SERVERPROPERTY('InstanceDefaultLogPath') AS LogPath", 
                        connection);

                    var pathsReader = await defaultPathsCommand.ExecuteReaderAsync();
                    await pathsReader.ReadAsync();
                    
                    var dataPath = pathsReader["DataPath"].ToString();
                    var logPath = pathsReader["LogPath"].ToString();
                    
                    pathsReader.Close();

                    // Restore full backup with NORECOVERY to allow applying differential and log backups
                    var restoreFullCommand = new SqlCommand(
                        $"RESTORE DATABASE [{targetDbName}] FROM DISK = '{fullBackupPath}' WITH NORECOVERY, " +
                        $"MOVE '{dataFileLogicalName}' TO '{Path.Combine(dataPath, targetDbName + ".mdf")}', " +
                        $"MOVE '{logFileLogicalName}' TO '{Path.Combine(logPath, targetDbName + "_log.ldf")}'", 
                        connection);

                    await restoreFullCommand.ExecuteNonQueryAsync();

                    // Apply differential backup if provided
                    if (!string.IsNullOrEmpty(differentialBackupPath))
                    {
                        var restoreDiffCommand = new SqlCommand(
                            $"RESTORE DATABASE [{targetDbName}] FROM DISK = '{differentialBackupPath}' WITH NORECOVERY", 
                            connection);

                        await restoreDiffCommand.ExecuteNonQueryAsync();
                    }

                    // Apply transaction log backups if provided
                    if (transactionLogBackupPaths != null && transactionLogBackupPaths.Length > 0)
                    {
                        foreach (var logBackupPath in transactionLogBackupPaths)
                        {
                            var restoreLogCommand = new SqlCommand(
                                $"RESTORE LOG [{targetDbName}] FROM DISK = '{logBackupPath}' WITH NORECOVERY", 
                                connection);

                            // If restoring to a point in time and this is the last log backup
                            if (restoreToPointInTime.HasValue && 
                                logBackupPath == transactionLogBackupPaths[transactionLogBackupPaths.Length - 1])
                            {
                                restoreLogCommand = new SqlCommand(
                                    $"RESTORE LOG [{targetDbName}] FROM DISK = '{logBackupPath}' WITH NORECOVERY, " +
                                    $"STOPAT = '{restoreToPointInTime.Value.ToString("yyyy-MM-dd HH:mm:ss")}'", 
                                    connection);
                            }

                            await restoreLogCommand.ExecuteNonQueryAsync();
                        }
                    }

                    // Recover the database to make it available
                    var recoverCommand = new SqlCommand(
                        $"RESTORE DATABASE [{targetDbName}] WITH RECOVERY", 
                        connection);

                    await recoverCommand.ExecuteNonQueryAsync();

                    // Set database back to multi user mode
                    var multiUserCommand = new SqlCommand(
                        $"ALTER DATABASE [{targetDbName}] SET MULTI_USER", 
                        connection);

                    await multiUserCommand.ExecuteNonQueryAsync();
                }

                _logger.LogInformation("Database {TargetDbName} restored successfully with delta backups", 
                    targetDbName);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring database {TargetDbName} with delta backups", 
                    newDatabaseName ?? settings.DatabaseName);
                return false;
            }
        }

        /// <summary>
        /// Gets the connection string for the specified database
        /// </summary>
        private string GetConnectionString(DatabaseSettings settings)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = $"{settings.ServerName}{(settings.Port > 0 ? "," + settings.Port : "")}",
                InitialCatalog = settings.DatabaseName,
                IntegratedSecurity = settings.UseIntegratedSecurity
            };

            if (!settings.UseIntegratedSecurity)
            {
                builder.UserID = settings.Username;
                builder.Password = settings.Password;
            }

            if (settings.UseSsl)
            {
                builder.Encrypt = true;
                builder.TrustServerCertificate = true;
            }

            return builder.ConnectionString;
        }

        /// <summary>
        /// Gets the connection string for the master database
        /// </summary>
        private string GetMasterConnectionString(DatabaseSettings settings)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = $"{settings.ServerName}{(settings.Port > 0 ? "," + settings.Port : "")}",
                InitialCatalog = "master",
                IntegratedSecurity = settings.UseIntegratedSecurity
            };

            if (!settings.UseIntegratedSecurity)
            {
                builder.UserID = settings.Username;
                builder.Password = settings.Password;
            }

            if (settings.UseSsl)
            {
                builder.Encrypt = true;
                builder.TrustServerCertificate = true;
            }

            return builder.ConnectionString;
        }
    }

    /// <summary>
    /// Interface for MSSQL delta backup service
    /// </summary>
    public interface IMsSqlDeltaBackupService
    {
        Task<string> PerformFullBackupAsync(DatabaseSettings settings, string outputPath);
        Task<string> PerformDifferentialBackupAsync(DatabaseSettings settings, string outputPath);
        Task<string> PerformTransactionLogBackupAsync(DatabaseSettings settings, string outputPath);
        Task<bool> RestoreFullBackupAsync(DatabaseSettings settings, string backupPath, string newDatabaseName = null);
        Task<bool> RestoreWithDeltaBackupsAsync(
            DatabaseSettings settings, 
            string fullBackupPath, 
            string differentialBackupPath = null, 
            string[] transactionLogBackupPaths = null, 
            string newDatabaseName = null,
            DateTime? restoreToPointInTime = null);
    }
}