using AI_BACKUP.WindowsService.Models;
using AI_BACKUP.WindowsService.Services;
using Microsoft.Extensions.Options;

namespace AI_BACKUP.WindowsService
{
    public class BackupWorker : BackgroundService
    {
        private readonly ILogger<BackupWorker> _logger;
        private readonly IApiClientService _apiClientService;
        private readonly IBackupService _backupService;
        private readonly ServiceSettings _settings;
        private readonly SemaphoreSlim _backupSemaphore;

        public BackupWorker(
            ILogger<BackupWorker> logger,
            IApiClientService apiClientService,
            IBackupService backupService,
            IOptions<ServiceSettings> settings)
        {
            _logger = logger;
            _apiClientService = apiClientService;
            _backupService = backupService;
            _settings = settings.Value;
            _backupSemaphore = new SemaphoreSlim(_settings.MaxConcurrentBackups);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Backup Worker starting at: {time}", DateTimeOffset.Now);

            try
            {
                // Register client on startup
                var registered = await _apiClientService.RegisterClientAsync();
                if (!registered)
                {
                    _logger.LogWarning("Failed to register client. Will retry later.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during client registration");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogDebug("Checking for pending backup jobs at: {time}", DateTimeOffset.Now);
                    
                    // Get pending backup jobs
                    var pendingJobs = await _apiClientService.GetPendingBackupJobsAsync();
                    
                    if (pendingJobs.Any())
                    {
                        _logger.LogInformation("Found {count} pending backup jobs", pendingJobs.Count);
                        
                        // Process each job
                        var tasks = pendingJobs.Select(job => ProcessBackupJobAsync(job, stoppingToken));
                        await Task.WhenAll(tasks);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing backup jobs");
                }

                // Wait for the next check interval
                await Task.Delay(TimeSpan.FromMinutes(_settings.BackupCheckIntervalMinutes), stoppingToken);
            }
        }

        private async Task ProcessBackupJobAsync(BackupJobModel job, CancellationToken stoppingToken)
        {
            try
            {
                // Acquire semaphore to limit concurrent backups
                await _backupSemaphore.WaitAsync(stoppingToken);
                
                try
                {
                    _logger.LogInformation("Starting backup job: {jobName} ({jobId})", job.Name, job.Id);
                    
                    // Update job status to Running
                    await _apiClientService.UpdateBackupJobStatusAsync(job.Id, "Running");
                    
                    // Perform the backup
                    var result = await _backupService.PerformBackupAsync(job, stoppingToken);
                    
                    // Upload the result
                    await _apiClientService.UploadBackupResultAsync(result);
                    
                    // Update job status based on result
                    await _apiClientService.UpdateBackupJobStatusAsync(
                        job.Id, 
                        result.Status == BackupRunStatus.Completed ? "Completed" : "Failed",
                        result.ErrorMessage);
                    
                    _logger.LogInformation("Completed backup job: {jobName} ({jobId}) with status: {status}", 
                        job.Name, job.Id, result.Status);
                }
                finally
                {
                    // Release semaphore
                    _backupSemaphore.Release();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Backup job cancelled: {jobName} ({jobId})", job.Name, job.Id);
                await _apiClientService.UpdateBackupJobStatusAsync(job.Id, "Cancelled", "Backup operation was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing backup job: {jobName} ({jobId})", job.Name, job.Id);
                await _apiClientService.UpdateBackupJobStatusAsync(job.Id, "Failed", ex.Message);
            }
        }
    }
}