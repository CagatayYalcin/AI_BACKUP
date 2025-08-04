using AI_BACKUP.WindowsService;
using AI_BACKUP.WindowsService.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Diagnostics;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "AI_BACKUP_Service";
    })
    .ConfigureServices((hostContext, services) =>
    {
        // Configuration
        var configuration = hostContext.Configuration;
        
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .CreateLogger();
        
        // Register services
        services.AddHttpClient();
        services.Configure<ServiceSettings>(configuration.GetSection("ServiceSettings"));
        
        // Register core services
        services.AddSingleton<ISystemInfoService, SystemInfoService>();
        services.AddSingleton<IApiClientService, ApiClientService>();
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<IFileSystemService, FileSystemService>();
        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddSingleton<ICompressionService, CompressionService>();
        services.AddSingleton<IDatabaseBackupService, DatabaseBackupService>();
        
        // Register cloud storage services
        services.AddSingleton<ICloudStorageService, GoogleDriveService>();
        services.AddSingleton<ICloudStorageService, OneDriveService>();
        services.AddSingleton<ICloudStorageService, FtpService>();
        services.AddSingleton<ICloudStorageService, SftpService>();
        
        // Register storage service (depends on cloud storage services)
        services.AddSingleton<IStorageService, StorageService>();
        
        // Register background services
        services.AddHostedService<BackupWorker>();
        services.AddHostedService<HeartbeatWorker>();
    })
    .UseSerilog()
    .Build();

try
{
    Log.Information("Starting AI_BACKUP Windows Service");
    await host.RunAsync();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "AI_BACKUP Windows Service terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}