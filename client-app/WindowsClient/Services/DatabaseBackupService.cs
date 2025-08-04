using AI_BACKUP.WindowsService.Models;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text;

namespace AI_BACKUP.WindowsService.Services
{
    public class DatabaseBackupService : IDatabaseBackupService
    {
        private readonly ILogger<DatabaseBackupService> _logger;
        private readonly ServiceSettings _settings;
        private readonly IFileSystemService _fileSystemService;

        public DatabaseBackupService(
            ILogger<DatabaseBackupService> logger,
            IOptions<ServiceSettings> settings,
            IFileSystemService fileSystemService)
        {
            _logger = logger;
            _settings = settings.Value;
            _fileSystemService = fileSystemService;
        }

        public async Task<string> BackupMsSqlDatabaseAsync(DatabaseSettings settings, string outputPath)
        {
            try
            {
                _logger.LogInformation("Backing up MSSQL database: {DatabaseName} to {OutputPath}", 
                    settings.DatabaseName, outputPath);
                
                // Ensure output directory exists
                var outputDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // Build connection string if not provided
                var connectionString = settings.ConnectionString;
                if (string.IsNullOrEmpty(connectionString))
                {
                    var builder = new StringBuilder();
                    builder.Append($"Server={settings.ServerName};");
                    builder.Append($"Database={settings.DatabaseName};");
                    
                    if (settings.UseIntegratedSecurity)
                    {
                        builder.Append("Integrated Security=True;");
                    }
                    else
                    {
                        builder.Append($"User Id={settings.Username};");
                        builder.Append($"Password={settings.Password};");
                    }
                    
                    if (settings.Port > 0)
                    {
                        builder.Append($"Port={settings.Port};");
                    }
                    
                    connectionString = builder.ToString();
                }

                // Create SQL backup script
                var backupScript = $@"
                    BACKUP DATABASE [{settings.DatabaseName}] 
                    TO DISK = N'{outputPath}' 
                    WITH NOFORMAT, NOINIT, NAME = N'{settings.DatabaseName}-Full Database Backup', 
                    SKIP, NOREWIND, NOUNLOAD, STATS = 10
                ";

                // Create temporary SQL script file
                var scriptPath = Path.Combine(
                    Path.GetDirectoryName(outputPath) ?? string.Empty, 
                    $"backup_{Guid.NewGuid():N}.sql");
                
                await File.WriteAllTextAsync(scriptPath, backupScript);

                try
                {
                    // Execute SQL script using sqlcmd
                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = "sqlcmd",
                        Arguments = $"-S {settings.ServerName} -d master -i \"{scriptPath}\"" + 
                                   (settings.UseIntegratedSecurity ? " -E" : $" -U {settings.Username} -P {settings.Password}"),
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = new Process { StartInfo = processStartInfo };
                    process.Start();
                    
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    
                    await process.WaitForExitAsync();
                    
                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"MSSQL backup failed with exit code {process.ExitCode}: {error}");
                    }

                    _logger.LogInformation("MSSQL backup completed successfully: {DatabaseName}", settings.DatabaseName);
                    return outputPath;
                }
                finally
                {
                    // Clean up script file
                    if (File.Exists(scriptPath))
                    {
                        File.Delete(scriptPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error backing up MSSQL database: {DatabaseName}", settings.DatabaseName);
                throw;
            }
        }

        public async Task<string> BackupMySqlDatabaseAsync(DatabaseSettings settings, string outputPath)
        {
            try
            {
                _logger.LogInformation("Backing up MySQL database: {DatabaseName} to {OutputPath}", 
                    settings.DatabaseName, outputPath);
                
                // Ensure output directory exists
                var outputDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // Build mysqldump command
                var arguments = new StringBuilder();
                
                // Add credentials
                if (!settings.UseIntegratedSecurity)
                {
                    arguments.Append($"-u {settings.Username} ");
                    
                    if (!string.IsNullOrEmpty(settings.Password))
                    {
                        arguments.Append($"-p{settings.Password} ");
                    }
                }
                
                // Add server and port
                arguments.Append($"-h {settings.ServerName} ");
                
                if (settings.Port > 0)
                {
                    arguments.Append($"-P {settings.Port} ");
                }
                
                // Add options
                if (settings.IncludeSchema && !settings.IncludeData)
                {
                    arguments.Append("--no-data ");
                }
                else if (!settings.IncludeSchema && settings.IncludeData)
                {
                    arguments.Append("--no-create-info ");
                }
                
                // Add database name and output file
                arguments.Append($"{settings.DatabaseName} -r \"{outputPath}\"");

                // Execute mysqldump command
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "mysqldump",
                    Arguments = arguments.ToString(),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processStartInfo };
                process.Start();
                
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                
                await process.WaitForExitAsync();
                
                if (process.ExitCode != 0)
                {
                    throw new Exception($"MySQL backup failed with exit code {process.ExitCode}: {error}");
                }

                _logger.LogInformation("MySQL backup completed successfully: {DatabaseName}", settings.DatabaseName);
                return outputPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error backing up MySQL database: {DatabaseName}", settings.DatabaseName);
                throw;
            }
        }

        public async Task<string> BackupPostgreSqlDatabaseAsync(DatabaseSettings settings, string outputPath)
        {
            try
            {
                _logger.LogInformation("Backing up PostgreSQL database: {DatabaseName} to {OutputPath}", 
                    settings.DatabaseName, outputPath);
                
                // Ensure output directory exists
                var outputDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // Build pg_dump command
                var arguments = new StringBuilder();
                
                // Add server and port
                arguments.Append($"-h {settings.ServerName} ");
                
                if (settings.Port > 0)
                {
                    arguments.Append($"-p {settings.Port} ");
                }
                
                // Add credentials
                if (!settings.UseIntegratedSecurity)
                {
                    arguments.Append($"-U {settings.Username} ");
                }
                
                // Add options
                if (settings.IncludeSchema && !settings.IncludeData)
                {
                    arguments.Append("--schema-only ");
                }
                else if (!settings.IncludeSchema && settings.IncludeData)
                {
                    arguments.Append("--data-only ");
                }
                
                // Add format
                arguments.Append("-Fc ");
                
                // Add database name and output file
                arguments.Append($"-f \"{outputPath}\" {settings.DatabaseName}");

                // Set environment variables for password
                var environmentVariables = new Dictionary<string, string>();
                if (!settings.UseIntegratedSecurity && !string.IsNullOrEmpty(settings.Password))
                {
                    environmentVariables["PGPASSWORD"] = settings.Password;
                }

                // Execute pg_dump command
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "pg_dump",
                    Arguments = arguments.ToString(),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                // Add environment variables
                foreach (var variable in environmentVariables)
                {
                    processStartInfo.Environment[variable.Key] = variable.Value;
                }

                using var process = new Process { StartInfo = processStartInfo };
                process.Start();
                
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                
                await process.WaitForExitAsync();
                
                if (process.ExitCode != 0)
                {
                    throw new Exception($"PostgreSQL backup failed with exit code {process.ExitCode}: {error}");
                }

                _logger.LogInformation("PostgreSQL backup completed successfully: {DatabaseName}", settings.DatabaseName);
                return outputPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error backing up PostgreSQL database: {DatabaseName}", settings.DatabaseName);
                throw;
            }
        }

        public async Task<string> BackupMongoDbDatabaseAsync(DatabaseSettings settings, string outputPath)
        {
            try
            {
                _logger.LogInformation("Backing up MongoDB database: {DatabaseName} to {OutputPath}", 
                    settings.DatabaseName, outputPath);
                
                // Ensure output directory exists
                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }

                // Build mongodump command
                var arguments = new StringBuilder();
                
                // Add server and port
                arguments.Append($"--host {settings.ServerName} ");
                
                if (settings.Port > 0)
                {
                    arguments.Append($"--port {settings.Port} ");
                }
                
                // Add credentials
                if (!settings.UseIntegratedSecurity && !string.IsNullOrEmpty(settings.Username))
                {
                    arguments.Append($"--username {settings.Username} ");
                    
                    if (!string.IsNullOrEmpty(settings.Password))
                    {
                        arguments.Append($"--password {settings.Password} ");
                    }
                }
                
                // Add database name and output directory
                arguments.Append($"--db {settings.DatabaseName} --out \"{outputPath}\"");

                // Execute mongodump command
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "mongodump",
                    Arguments = arguments.ToString(),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processStartInfo };
                process.Start();
                
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                
                await process.WaitForExitAsync();
                
                if (process.ExitCode != 0)
                {
                    throw new Exception($"MongoDB backup failed with exit code {process.ExitCode}: {error}");
                }

                _logger.LogInformation("MongoDB backup completed successfully: {DatabaseName}", settings.DatabaseName);
                return outputPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error backing up MongoDB database: {DatabaseName}", settings.DatabaseName);
                throw;
            }
        }

        public async Task<bool> RestoreMsSqlDatabaseAsync(string backupFilePath, DatabaseSettings settings)
        {
            try
            {
                _logger.LogInformation("Restoring MSSQL database: {DatabaseName} from {BackupFilePath}", 
                    settings.DatabaseName, backupFilePath);
                
                // Check if backup file exists
                if (!await _fileSystemService.FileExistsAsync(backupFilePath))
                {
                    throw new FileNotFoundException($"Backup file not found: {backupFilePath}");
                }

                // Create SQL restore script
                var restoreScript = $@"
                    RESTORE DATABASE [{settings.DatabaseName}] 
                    FROM DISK = N'{backupFilePath}' 
                    WITH FILE = 1, NOUNLOAD, REPLACE, STATS = 10
                ";

                // Create temporary SQL script file
                var scriptPath = Path.Combine(
                    Path.GetDirectoryName(backupFilePath) ?? string.Empty, 
                    $"restore_{Guid.NewGuid():N}.sql");
                
                await File.WriteAllTextAsync(scriptPath, restoreScript);

                try
                {
                    // Execute SQL script using sqlcmd
                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = "sqlcmd",
                        Arguments = $"-S {settings.ServerName} -d master -i \"{scriptPath}\"" + 
                                   (settings.UseIntegratedSecurity ? " -E" : $" -U {settings.Username} -P {settings.Password}"),
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = new Process { StartInfo = processStartInfo };
                    process.Start();
                    
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    
                    await process.WaitForExitAsync();
                    
                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"MSSQL restore failed with exit code {process.ExitCode}: {error}");
                    }

                    _logger.LogInformation("MSSQL restore completed successfully: {DatabaseName}", settings.DatabaseName);
                    return true;
                }
                finally
                {
                    // Clean up script file
                    if (File.Exists(scriptPath))
                    {
                        File.Delete(scriptPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring MSSQL database: {DatabaseName}", settings.DatabaseName);
                return false;
            }
        }

        public async Task<bool> RestoreMySqlDatabaseAsync(string backupFilePath, DatabaseSettings settings)
        {
            try
            {
                _logger.LogInformation("Restoring MySQL database: {DatabaseName} from {BackupFilePath}", 
                    settings.DatabaseName, backupFilePath);
                
                // Check if backup file exists
                if (!await _fileSystemService.FileExistsAsync(backupFilePath))
                {
                    throw new FileNotFoundException($"Backup file not found: {backupFilePath}");
                }

                // Build mysql command
                var arguments = new StringBuilder();
                
                // Add credentials
                if (!settings.UseIntegratedSecurity)
                {
                    arguments.Append($"-u {settings.Username} ");
                    
                    if (!string.IsNullOrEmpty(settings.Password))
                    {
                        arguments.Append($"-p{settings.Password} ");
                    }
                }
                
                // Add server and port
                arguments.Append($"-h {settings.ServerName} ");
                
                if (settings.Port > 0)
                {
                    arguments.Append($"-P {settings.Port} ");
                }
                
                // Add database name
                arguments.Append($"{settings.DatabaseName}");

                // Execute mysql command with input from backup file
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "mysql",
                    Arguments = arguments.ToString(),
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processStartInfo };
                process.Start();
                
                // Read backup file and send to mysql process
                var backupContent = await File.ReadAllTextAsync(backupFilePath);
                await process.StandardInput.WriteAsync(backupContent);
                process.StandardInput.Close();
                
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                
                await process.WaitForExitAsync();
                
                if (process.ExitCode != 0)
                {
                    throw new Exception($"MySQL restore failed with exit code {process.ExitCode}: {error}");
                }

                _logger.LogInformation("MySQL restore completed successfully: {DatabaseName}", settings.DatabaseName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring MySQL database: {DatabaseName}", settings.DatabaseName);
                return false;
            }
        }

        public async Task<bool> RestorePostgreSqlDatabaseAsync(string backupFilePath, DatabaseSettings settings)
        {
            try
            {
                _logger.LogInformation("Restoring PostgreSQL database: {DatabaseName} from {BackupFilePath}", 
                    settings.DatabaseName, backupFilePath);
                
                // Check if backup file exists
                if (!await _fileSystemService.FileExistsAsync(backupFilePath))
                {
                    throw new FileNotFoundException($"Backup file not found: {backupFilePath}");
                }

                // Build pg_restore command
                var arguments = new StringBuilder();
                
                // Add server and port
                arguments.Append($"-h {settings.ServerName} ");
                
                if (settings.Port > 0)
                {
                    arguments.Append($"-p {settings.Port} ");
                }
                
                // Add credentials
                if (!settings.UseIntegratedSecurity)
                {
                    arguments.Append($"-U {settings.Username} ");
                }
                
                // Add options
                arguments.Append("-c -C ");
                
                // Add database name and input file
                arguments.Append($"-d postgres \"{backupFilePath}\"");

                // Set environment variables for password
                var environmentVariables = new Dictionary<string, string>();
                if (!settings.UseIntegratedSecurity && !string.IsNullOrEmpty(settings.Password))
                {
                    environmentVariables["PGPASSWORD"] = settings.Password;
                }

                // Execute pg_restore command
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "pg_restore",
                    Arguments = arguments.ToString(),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                // Add environment variables
                foreach (var variable in environmentVariables)
                {
                    processStartInfo.Environment[variable.Key] = variable.Value;
                }

                using var process = new Process { StartInfo = processStartInfo };
                process.Start();
                
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                
                await process.WaitForExitAsync();
                
                if (process.ExitCode != 0)
                {
                    throw new Exception($"PostgreSQL restore failed with exit code {process.ExitCode}: {error}");
                }

                _logger.LogInformation("PostgreSQL restore completed successfully: {DatabaseName}", settings.DatabaseName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring PostgreSQL database: {DatabaseName}", settings.DatabaseName);
                return false;
            }
        }

        public async Task<bool> RestoreMongoDbDatabaseAsync(string backupFilePath, DatabaseSettings settings)
        {
            try
            {
                _logger.LogInformation("Restoring MongoDB database: {DatabaseName} from {BackupFilePath}", 
                    settings.DatabaseName, backupFilePath);
                
                // Check if backup directory exists
                if (!await _fileSystemService.DirectoryExistsAsync(backupFilePath))
                {
                    throw new DirectoryNotFoundException($"Backup directory not found: {backupFilePath}");
                }

                // Build mongorestore command
                var arguments = new StringBuilder();
                
                // Add server and port
                arguments.Append($"--host {settings.ServerName} ");
                
                if (settings.Port > 0)
                {
                    arguments.Append($"--port {settings.Port} ");
                }
                
                // Add credentials
                if (!settings.UseIntegratedSecurity && !string.IsNullOrEmpty(settings.Username))
                {
                    arguments.Append($"--username {settings.Username} ");
                    
                    if (!string.IsNullOrEmpty(settings.Password))
                    {
                        arguments.Append($"--password {settings.Password} ");
                    }
                }
                
                // Add options
                arguments.Append("--drop ");
                
                // Add database name and input directory
                arguments.Append($"--db {settings.DatabaseName} \"{backupFilePath}/{settings.DatabaseName}\"");

                // Execute mongorestore command
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "mongorestore",
                    Arguments = arguments.ToString(),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processStartInfo };
                process.Start();
                
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                
                await process.WaitForExitAsync();
                
                if (process.ExitCode != 0)
                {
                    throw new Exception($"MongoDB restore failed with exit code {process.ExitCode}: {error}");
                }

                _logger.LogInformation("MongoDB restore completed successfully: {DatabaseName}", settings.DatabaseName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring MongoDB database: {DatabaseName}", settings.DatabaseName);
                return false;
            }
        }

        public async Task<bool> TestConnectionAsync(BackupType databaseType, DatabaseSettings settings)
        {
            try
            {
                _logger.LogInformation("Testing connection to {DatabaseType} database: {DatabaseName}", 
                    databaseType, settings.DatabaseName);
                
                switch (databaseType)
                {
                    case BackupType.MSSQLDatabase:
                        return await TestMsSqlConnectionAsync(settings);
                    case BackupType.MySQLDatabase:
                        return await TestMySqlConnectionAsync(settings);
                    case BackupType.PostgreSQLDatabase:
                        return await TestPostgreSqlConnectionAsync(settings);
                    case BackupType.MongoDBDatabase:
                        return await TestMongoDbConnectionAsync(settings);
                    default:
                        throw new NotSupportedException($"Database type {databaseType} is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connection to {DatabaseType} database: {DatabaseName}", 
                    databaseType, settings.DatabaseName);
                return false;
            }
        }

        private async Task<bool> TestMsSqlConnectionAsync(DatabaseSettings settings)
        {
            // Create SQL test script
            var testScript = "SELECT 1";

            // Create temporary SQL script file
            var scriptPath = Path.Combine(
                Path.GetTempPath(), 
                $"test_{Guid.NewGuid():N}.sql");
            
            await File.WriteAllTextAsync(scriptPath, testScript);

            try
            {
                // Execute SQL script using sqlcmd
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "sqlcmd",
                    Arguments = $"-S {settings.ServerName} -d {settings.DatabaseName} -i \"{scriptPath}\"" + 
                               (settings.UseIntegratedSecurity ? " -E" : $" -U {settings.Username} -P {settings.Password}"),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processStartInfo };
                process.Start();
                
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                
                await process.WaitForExitAsync();
                
                return process.ExitCode == 0;
            }
            finally
            {
                // Clean up script file
                if (File.Exists(scriptPath))
                {
                    File.Delete(scriptPath);
                }
            }
        }

        private async Task<bool> TestMySqlConnectionAsync(DatabaseSettings settings)
        {
            // Build mysql command
            var arguments = new StringBuilder();
            
            // Add credentials
            if (!settings.UseIntegratedSecurity)
            {
                arguments.Append($"-u {settings.Username} ");
                
                if (!string.IsNullOrEmpty(settings.Password))
                {
                    arguments.Append($"-p{settings.Password} ");
                }
            }
            
            // Add server and port
            arguments.Append($"-h {settings.ServerName} ");
            
            if (settings.Port > 0)
            {
                arguments.Append($"-P {settings.Port} ");
            }
            
            // Add database name and test query
            arguments.Append($"{settings.DatabaseName} -e \"SELECT 1\"");

            // Execute mysql command
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "mysql",
                Arguments = arguments.ToString(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();
            
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync();
            
            return process.ExitCode == 0;
        }

        private async Task<bool> TestPostgreSqlConnectionAsync(DatabaseSettings settings)
        {
            // Build psql command
            var arguments = new StringBuilder();
            
            // Add server and port
            arguments.Append($"-h {settings.ServerName} ");
            
            if (settings.Port > 0)
            {
                arguments.Append($"-p {settings.Port} ");
            }
            
            // Add credentials
            if (!settings.UseIntegratedSecurity)
            {
                arguments.Append($"-U {settings.Username} ");
            }
            
            // Add database name and test query
            arguments.Append($"-d {settings.DatabaseName} -c \"SELECT 1\"");

            // Set environment variables for password
            var environmentVariables = new Dictionary<string, string>();
            if (!settings.UseIntegratedSecurity && !string.IsNullOrEmpty(settings.Password))
            {
                environmentVariables["PGPASSWORD"] = settings.Password;
            }

            // Execute psql command
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "psql",
                Arguments = arguments.ToString(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            // Add environment variables
            foreach (var variable in environmentVariables)
            {
                processStartInfo.Environment[variable.Key] = variable.Value;
            }

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();
            
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync();
            
            return process.ExitCode == 0;
        }

        private async Task<bool> TestMongoDbConnectionAsync(DatabaseSettings settings)
        {
            // Build mongo command
            var arguments = new StringBuilder();
            
            // Add server and port
            arguments.Append($"--host {settings.ServerName} ");
            
            if (settings.Port > 0)
            {
                arguments.Append($"--port {settings.Port} ");
            }
            
            // Add credentials
            if (!settings.UseIntegratedSecurity && !string.IsNullOrEmpty(settings.Username))
            {
                arguments.Append($"--username {settings.Username} ");
                
                if (!string.IsNullOrEmpty(settings.Password))
                {
                    arguments.Append($"--password {settings.Password} ");
                }
            }
            
            // Add database name and test query
            arguments.Append($"{settings.DatabaseName} --eval \"db.stats()\"");

            // Execute mongo command
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "mongo",
                Arguments = arguments.ToString(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();
            
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync();
            
            return process.ExitCode == 0;
        }
    }
}