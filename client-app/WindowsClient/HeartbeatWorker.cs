using AI_BACKUP.WindowsService.Services;
using Microsoft.Extensions.Options;

namespace AI_BACKUP.WindowsService
{
    public class HeartbeatWorker : BackgroundService
    {
        private readonly ILogger<HeartbeatWorker> _logger;
        private readonly IApiClientService _apiClientService;
        private readonly ISystemInfoService _systemInfoService;
        private readonly ServiceSettings _settings;

        public HeartbeatWorker(
            ILogger<HeartbeatWorker> logger,
            IApiClientService apiClientService,
            ISystemInfoService systemInfoService,
            IOptions<ServiceSettings> settings)
        {
            _logger = logger;
            _apiClientService = apiClientService;
            _systemInfoService = systemInfoService;
            _settings = settings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Heartbeat Worker starting at: {time}", DateTimeOffset.Now);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogDebug("Sending heartbeat at: {time}", DateTimeOffset.Now);
                    
                    // Get system information
                    var systemInfo = _systemInfoService.GetSystemInfo();
                    
                    // Send heartbeat
                    var success = await _apiClientService.SendHeartbeatAsync(systemInfo);
                    
                    if (!success)
                    {
                        _logger.LogWarning("Failed to send heartbeat");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending heartbeat");
                }

                // Wait for the next heartbeat interval
                await Task.Delay(TimeSpan.FromSeconds(_settings.HeartbeatIntervalSeconds), stoppingToken);
            }
        }
    }
}