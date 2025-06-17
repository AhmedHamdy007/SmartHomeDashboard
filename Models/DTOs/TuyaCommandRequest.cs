using System.Text.Json.Serialization;

namespace SmartHomeDashboard.Models.DTOs
{
    public class TuyaCommand
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;
        
        [JsonPropertyName("value")]
        public object Value { get; set; } = new();
    }

    public class TuyaCommandRequest
    {
        [JsonPropertyName("commands")]
        public List<TuyaCommand> Commands { get; set; } = new();
    }

    public class TuyaCommandResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("result")]
        public bool Result { get; set; }
        
        [JsonPropertyName("msg")]
        public string Message { get; set; } = string.Empty;
    }
}