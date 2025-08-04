# AI_BACKUP Windows Client Service

This is the Windows client service component of the AI_BACKUP solution. It runs as a Windows service and handles backup operations on Windows client machines.

## Features

- Runs as a Windows service
- Performs file, directory, and database backups
- Supports MSSQL, MySQL, PostgreSQL, and MongoDB databases
- Encrypts and compresses backup data
- Uploads backups to various storage providers (local, Google Drive, OneDrive, etc.)
- Sends heartbeat signals to the server
- Monitors system resources

## Configuration

The service is configured through the `appsettings.json` file. Here are the main configuration options:

```json
{
  "ServiceSettings": {
    "ApiBaseUrl": "https://api.ai-backup.com",
    "ClientId": "",
    "ClientSecret": "",
    "HeartbeatIntervalSeconds": 60,
    "BackupCheckIntervalMinutes": 5,
    "TempDirectory": "C:\\AI_BACKUP\\Temp",
    "LogDirectory": "C:\\AI_BACKUP\\Logs",
    "EnableEncryption": true,
    "EncryptionKey": "",
    "MaxConcurrentBackups": 2,
    "MaxRetryAttempts": 3,
    "RetryDelaySeconds": 30
  }
}
```

## Installation

1. Build the service using Visual Studio or the .NET CLI:
   ```
   dotnet publish -c Release -r win-x64 --self-contained
   ```

2. Install the service using the Windows Service Control Manager:
   ```
   sc create AI_BACKUP_Service binPath= "C:\path\to\AI_BACKUP.WindowsService.exe"
   sc description AI_BACKUP_Service "AI_BACKUP backup service for Windows"
   sc start AI_BACKUP_Service
   ```

3. Configure the service by editing the `appsettings.json` file in the installation directory.

## Development

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 or later (optional)

### Building

```
dotnet build
```

### Running in Development Mode

```
dotnet run
```

### Debugging

The service can be debugged by running it as a console application in development mode. Set the environment variable `DOTNET_ENVIRONMENT` to `Development` to use the development configuration.

## Architecture

The service is built using the following components:

- **BackupWorker**: Background service that checks for pending backup jobs and executes them
- **HeartbeatWorker**: Background service that sends heartbeat signals to the server
- **ApiClientService**: Communicates with the server API
- **BackupService**: Performs backup operations
- **StorageService**: Handles uploading backups to storage providers
- **EncryptionService**: Encrypts and decrypts backup data
- **CompressionService**: Compresses and decompresses backup data
- **FileSystemService**: Handles file system operations
- **DatabaseBackupService**: Performs database backup operations
- **SystemInfoService**: Collects system information

## License

This project is licensed under the terms of the license included in the repository.