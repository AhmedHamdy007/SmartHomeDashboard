using SmartHomeDashboard.Models.Entities;
using SmartHomeDashboard.Models.DTOs;

namespace SmartHomeDashboard.Services
{
    public interface IDeviceService
    {
        Task<List<Device>> GetUserDevicesAsync(string userId);
        Task<Device?> GetDeviceAsync(int deviceId);
        Task<Device?> GetDeviceByTuyaIdAsync(string tuyaDeviceId);
        Task<User?> GetUserWithTuyaTokenAsync(string userId);
        Task SyncDevicesAsync(string userId, List<TuyaDevice> tuyaDevices);
        Task UpdateDeviceStatusAsync(string tuyaDeviceId, List<TuyaDeviceStatus> status);
        Task LogDeviceActionAsync(string tuyaDeviceId, string command, string value);
        Task ProcessAutomationRulesAsync(string tuyaDeviceId, List<TuyaDeviceStatus> status);
        Task<List<DeviceLog>> GetDeviceLogsAsync(string userId, int? deviceId = null);
        Task<List<Notification>> GetUserNotificationsAsync(string userId);
        Task CreateNotificationAsync(string userId, string title, string message, string type);
    }
}