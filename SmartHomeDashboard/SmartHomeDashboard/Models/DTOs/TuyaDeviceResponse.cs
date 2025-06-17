using System.Text.Json.Serialization;

namespace SmartHomeDashboard.Models.DTOs
{
    public class TuyaDeviceListResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("result")]
        public List<TuyaDevice> Result { get; set; } = new();
    }

    public class TuyaDevice
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;
        
        [JsonPropertyName("product_id")]
        public string ProductId { get; set; } = string.Empty;
        
        [JsonPropertyName("product_name")]
        public string ProductName { get; set; } = string.Empty;
        
        [JsonPropertyName("online")]
        public bool Online { get; set; }
        
        [JsonPropertyName("status")]
        public List<TuyaDeviceStatus> Status { get; set; } = new();
        
        [JsonPropertyName("icon")]
        public string Icon { get; set; } = string.Empty;
    }

    public class TuyaDeviceStatus
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;
        
        [JsonPropertyName("value")]
        public object Value { get; set; } = new();
    }

    public class TuyaDeviceStatusResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("result")]
        public List<TuyaDeviceStatus> Result { get; set; } = new();
    }
}