using AI_BACKUP.WindowsService.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace AI_BACKUP.WindowsService.Services
{
    public class ApiClientService : IApiClientService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiClientService> _logger;
        private readonly ServiceSettings _settings;
        private readonly ISystemInfoService _systemInfoService;
        private string? _authToken;
        private DateTime _tokenExpiration = DateTime.MinValue;

        public ApiClientService(
            HttpClient httpClient,
            ILogger<ApiClientService> logger,
            IOptions<ServiceSettings> settings,
            ISystemInfoService systemInfoService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _settings = settings.Value;
            _systemInfoService = systemInfoService;

            _httpClient.BaseAddress = new Uri(_settings.ApiBaseUrl);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<bool> AuthenticateAsync()
        {
            try
            {
                var authRequest = new
                {
                    ClientId = _settings.ClientId,
                    ClientSecret = _settings.ClientSecret,
                    MachineName = _systemInfoService.GetMachineName(),
                    MacAddress = _systemInfoService.GetMacAddress()
                };

                var content = new StringContent(JsonConvert.SerializeObject(authRequest), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/auth/client", content);

                if (response.IsSuccessStatusCode)
                {
                    var authResult = JsonConvert.DeserializeObject<AuthResult>(
                        await response.Content.ReadAsStringAsync());

                    if (authResult != null)
                    {
                        _authToken = authResult.Token;
                        _tokenExpiration = DateTime.UtcNow.AddSeconds(authResult.ExpiresIn);
                        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);
                        return true;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Authentication failed: {StatusCode} - {ErrorContent}", 
                        response.StatusCode, errorContent);
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authentication");
                return false;
            }
        }

        public async Task<bool> IsAuthenticated()
        {
            if (string.IsNullOrEmpty(_authToken) || DateTime.UtcNow >= _tokenExpiration.AddMinutes(-5))
            {
                return await AuthenticateAsync();
            }

            return true;
        }

        public async Task<bool> RegisterClientAsync()
        {
            if (!await IsAuthenticated())
            {
                return false;
            }

            try
            {
                var systemInfo = _systemInfoService.GetSystemInfo();
                var registrationRequest = new
                {
                    MachineName = _systemInfoService.GetMachineName(),
                    OperatingSystem = _systemInfoService.GetOperatingSystem(),
                    MacAddress = _systemInfoService.GetMacAddress(),
                    IpAddress = _systemInfoService.GetIpAddress(),
                    SystemInfo = systemInfo,
                    ServiceVersion = _systemInfoService.GetServiceVersion()
                };

                var content = new StringContent(JsonConvert.SerializeObject(registrationRequest), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/clients/register", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Client registered successfully");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Client registration failed: {StatusCode} - {ErrorContent}", 
                        response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during client registration");
                return false;
            }
        }

        public async Task<bool> SendHeartbeatAsync(Dictionary<string, object> systemInfo)
        {
            if (!await IsAuthenticated())
            {
                return false;
            }

            try
            {
                var heartbeatRequest = new
                {
                    MachineName = _systemInfoService.GetMachineName(),
                    MacAddress = _systemInfoService.GetMacAddress(),
                    Timestamp = DateTime.UtcNow,
                    SystemInfo = systemInfo
                };

                var content = new StringContent(JsonConvert.SerializeObject(heartbeatRequest), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/clients/heartbeat", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Heartbeat sent successfully");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Heartbeat failed: {StatusCode} - {ErrorContent}", 
                        response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending heartbeat");
                return false;
            }
        }

        public async Task<List<BackupJobModel>> GetPendingBackupJobsAsync()
        {
            if (!await IsAuthenticated())
            {
                return new List<BackupJobModel>();
            }

            try
            {
                var response = await _httpClient.GetAsync("/api/backup-jobs/pending");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var jobs = JsonConvert.DeserializeObject<List<BackupJobModel>>(content);
                    return jobs ?? new List<BackupJobModel>();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to get pending backup jobs: {StatusCode} - {ErrorContent}", 
                        response.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending backup jobs");
            }

            return new List<BackupJobModel>();
        }

        public async Task<bool> UpdateBackupJobStatusAsync(Guid backupJobId, string status, string? message = null)
        {
            if (!await IsAuthenticated())
            {
                return false;
            }

            try
            {
                var updateRequest = new
                {
                    Status = status,
                    Message = message,
                    Timestamp = DateTime.UtcNow
                };

                var content = new StringContent(JsonConvert.SerializeObject(updateRequest), Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"/api/backup-jobs/{backupJobId}/status", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Backup job status updated successfully: {BackupJobId} - {Status}", 
                        backupJobId, status);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to update backup job status: {StatusCode} - {ErrorContent}", 
                        response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating backup job status for job {BackupJobId}", backupJobId);
                return false;
            }
        }

        public async Task<bool> UploadBackupResultAsync(BackupResultModel result)
        {
            if (!await IsAuthenticated())
            {
                return false;
            }

            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(result), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"/api/backup-jobs/{result.BackupJobId}/results", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Backup result uploaded successfully: {BackupJobId}", result.BackupJobId);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to upload backup result: {StatusCode} - {ErrorContent}", 
                        response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading backup result for job {BackupJobId}", result.BackupJobId);
                return false;
            }
        }

        public async Task<StorageConfigModel?> GetStorageConfigAsync(Guid storageConfigId)
        {
            if (!await IsAuthenticated())
            {
                return null;
            }

            try
            {
                var response = await _httpClient.GetAsync($"/api/storage-configs/{storageConfigId}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<StorageConfigModel>(content);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to get storage config: {StatusCode} - {ErrorContent}", 
                        response.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting storage config {StorageConfigId}", storageConfigId);
            }

            return null;
        }

        private class AuthResult
        {
            public string Token { get; set; } = string.Empty;
            public int ExpiresIn { get; set; }
        }
    }
}