using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;

namespace AI_BACKUP.WindowsService.Services
{
    public class SystemInfoService : ISystemInfoService
    {
        private readonly ILogger<SystemInfoService> _logger;

        public SystemInfoService(ILogger<SystemInfoService> logger)
        {
            _logger = logger;
        }

        public string GetMachineName()
        {
            return Environment.MachineName;
        }

        public string GetOperatingSystem()
        {
            return RuntimeInformation.OSDescription;
        }

        public string GetMacAddress()
        {
            try
            {
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                var activeInterface = networkInterfaces.FirstOrDefault(ni => 
                    ni.OperationalStatus == OperationalStatus.Up && 
                    ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);

                if (activeInterface != null)
                {
                    var physicalAddress = activeInterface.GetPhysicalAddress();
                    return string.Join(":", physicalAddress.GetAddressBytes()
                        .Select(b => b.ToString("X2")));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting MAC address");
            }

            return string.Empty;
        }

        public string GetIpAddress()
        {
            try
            {
                var hostName = Dns.GetHostName();
                var hostEntry = Dns.GetHostEntry(hostName);
                var ipAddress = hostEntry.AddressList
                    .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

                return ipAddress?.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting IP address");
                return string.Empty;
            }
        }

        public double GetAvailableDiskSpace(string drivePath)
        {
            try
            {
                var driveInfo = new DriveInfo(drivePath);
                return Math.Round(driveInfo.AvailableFreeSpace / (1024.0 * 1024 * 1024), 2); // GB
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available disk space for {DrivePath}", drivePath);
                return 0;
            }
        }

        public double GetTotalDiskSpace(string drivePath)
        {
            try
            {
                var driveInfo = new DriveInfo(drivePath);
                return Math.Round(driveInfo.TotalSize / (1024.0 * 1024 * 1024), 2); // GB
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total disk space for {DrivePath}", drivePath);
                return 0;
            }
        }

        public double GetCpuUsage()
        {
            try
            {
                using var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                cpuCounter.NextValue(); // First call will always return 0
                Thread.Sleep(1000); // Wait for a second
                return Math.Round(cpuCounter.NextValue(), 2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting CPU usage");
                return 0;
            }
        }

        public double GetMemoryUsage()
        {
            try
            {
                using var ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                var availableMemoryMB = ramCounter.NextValue();
                var totalMemoryMB = GetTotalMemory();
                var usedMemoryMB = totalMemoryMB - availableMemoryMB;
                return Math.Round((usedMemoryMB / totalMemoryMB) * 100, 2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting memory usage");
                return 0;
            }
        }

        public double GetTotalMemory()
        {
            try
            {
                using var ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                using var commitCounter = new PerformanceCounter("Memory", "Committed Bytes");
                
                var availableMemoryMB = ramCounter.NextValue();
                var committedBytes = commitCounter.NextValue();
                var committedMemoryMB = committedBytes / (1024 * 1024);
                
                return Math.Round(availableMemoryMB + committedMemoryMB, 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total memory");
                return 0;
            }
        }

        public string GetServiceVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
        }

        public Dictionary<string, object> GetSystemInfo()
        {
            var systemDrive = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
            
            return new Dictionary<string, object>
            {
                { "MachineName", GetMachineName() },
                { "OperatingSystem", GetOperatingSystem() },
                { "MacAddress", GetMacAddress() },
                { "IpAddress", GetIpAddress() },
                { "AvailableDiskSpaceGB", GetAvailableDiskSpace(systemDrive) },
                { "TotalDiskSpaceGB", GetTotalDiskSpace(systemDrive) },
                { "CpuUsagePercent", GetCpuUsage() },
                { "MemoryUsagePercent", GetMemoryUsage() },
                { "TotalMemoryMB", GetTotalMemory() },
                { "ServiceVersion", GetServiceVersion() },
                { "LastUpdated", DateTime.UtcNow }
            };
        }
    }
}