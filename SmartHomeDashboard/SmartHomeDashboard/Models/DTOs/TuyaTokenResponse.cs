using System.Text.Json.Serialization;

namespace SmartHomeDashboard.Models.DTOs
{
    public class TuyaTokenResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("result")]
        public TuyaTokenResult Result { get; set; } = new();
        
        [JsonPropertyName("msg")]
        public string Message { get; set; } = string.Empty;
    }

    public class TuyaTokenResult
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
        
        [JsonPropertyName("expire_time")]
        public int ExpireTime { get; set; }
        
        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;
        
        [JsonPropertyName("uid")]
        public string Uid { get; set; } = string.Empty;
    }
}