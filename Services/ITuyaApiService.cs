using SmartHomeDashboard.Models.DTOs;

namespace SmartHomeDashboard.Services
{
    public interface ITuyaApiService
    {
        Task<string> GetAccessTokenAsync();
        Task<List<TuyaDevice>> GetUserDevicesAsync(string userId);
        Task<List<TuyaDeviceStatus>> GetDeviceStatusAsync(string deviceId);
        Task<bool> SendDeviceCommandAsync(string deviceId, List<TuyaCommand> commands);
        Task<List<TuyaDeviceFunction>> GetDeviceFunctionsAsync(string deviceId);
        Task<bool> RegisterWebhookAsync(string callbackUrl);
        Task<string> GetTuyaAuthUrlAsync(string state);
        Task<TuyaTokenResponse> ExchangeCodeForTokenAsync(string code);
    }

    public class TuyaDeviceFunction
    {
        public string Code { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public object Values { get; set; } = new();
    }
}