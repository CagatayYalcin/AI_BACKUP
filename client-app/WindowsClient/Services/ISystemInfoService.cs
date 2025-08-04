namespace AI_BACKUP.WindowsService.Services
{
    public interface ISystemInfoService
    {
        string GetMachineName();
        string GetOperatingSystem();
        string GetMacAddress();
        string GetIpAddress();
        double GetAvailableDiskSpace(string drivePath);
        double GetTotalDiskSpace(string drivePath);
        double GetCpuUsage();
        double GetMemoryUsage();
        double GetTotalMemory();
        string GetServiceVersion();
        Dictionary<string, object> GetSystemInfo();
    }
}