using System.Text.Json.Serialization;

namespace SmartHomeDashboard.Models.DTOs
{
    public class TuyaWebhookPayload
    {
        [JsonPropertyName("dataId")]
        public string DataId { get; set; } = string.Empty;
        
        [JsonPropertyName("devId")]
        public string DeviceId { get; set; } = string.Empty;
        
        [JsonPropertyName("productKey")]
        public string ProductKey { get; set; } = string.Empty;
        
        [JsonPropertyName("status")]
        public List<TuyaDeviceStatus> Status { get; set; } = new();
        
        [JsonPropertyName("ts")]
        public long Timestamp { get; set; }
    }
}