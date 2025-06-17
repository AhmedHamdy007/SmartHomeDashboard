using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SmartHomeDashboard.Models.DTOs;

namespace SmartHomeDashboard.Services
{
    public class TuyaApiService : ITuyaApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TuyaApiService> _logger;
        private string? _accessToken;
        private DateTime _tokenExpiry;

        public TuyaApiService(HttpClient httpClient, IConfiguration configuration, ILogger<TuyaApiService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            
            _httpClient.BaseAddress = new Uri(_configuration["TuyaApi:BaseUrl"]!);
        }

        // 🔑 API INTEGRATION POINT 1: Get Access Token
        public async Task<string> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
                return _accessToken;

            var clientId = _configuration["TuyaApi:ClientId"];
            var clientSecret = _configuration["TuyaApi:ClientSecret"];
            
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            var signString = clientId + timestamp;
            var sign = ComputeHmacSha256(signString, clientSecret!);

            var request = new HttpRequestMessage(HttpMethod.Get, "/v1.0/token?grant_type=1");
            request.Headers.Add("client_id", clientId);
            request.Headers.Add("sign", sign);
            request.Headers.Add("t", timestamp);
            request.Headers.Add("sign_method", "HMAC-SHA256");

            try
            {
                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation("Tuya token response: {Content}", content);
                
                var tokenResponse = JsonSerializer.Deserialize<TuyaTokenResponse>(content);

                if (tokenResponse?.Success == true && tokenResponse.Result != null)
                {
                    _accessToken = tokenResponse.Result.AccessToken;
                    _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.Result.ExpireTime - 300); // 5 min buffer
                    return _accessToken;
                }

                throw new Exception($"Failed to get access token: {tokenResponse?.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Tuya access token");
                throw;
            }
        }

        // 🔑 API INTEGRATION POINT 2: Get User Devices
        public async Task<List<TuyaDevice>> GetUserDevicesAsync(string userId)
        {
            var token = await GetAccessTokenAsync();
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            
            var request = new HttpRequestMessage(HttpMethod.Get, $"/v1.0/users/{userId}/devices");
            AddAuthHeaders(request, token, timestamp);

            try
            {
                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation("Get devices response: {Content}", content);
                
                var deviceResponse = JsonSerializer.Deserialize<TuyaDeviceListResponse>(content);
                return deviceResponse?.Result ?? new List<TuyaDevice>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user devices for user {UserId}", userId);
                return new List<TuyaDevice>();
            }
        }

        // 🔑 API INTEGRATION POINT 3: Get Device Status
        public async Task<List<TuyaDeviceStatus>> GetDeviceStatusAsync(string deviceId)
        {
            var token = await GetAccessTokenAsync();
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            
            var request = new HttpRequestMessage(HttpMethod.Get, $"/v1.0/devices/{deviceId}/status");
            AddAuthHeaders(request, token, timestamp);

            try
            {
                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                
                var statusResponse = JsonSerializer.Deserialize<TuyaDeviceStatusResponse>(content);
                return statusResponse?.Result ?? new List<TuyaDeviceStatus>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting device status for device {DeviceId}", deviceId);
                return new List<TuyaDeviceStatus>();
            }
        }

        // 🔑 API INTEGRATION POINT 4: Send Device Command
        public async Task<bool> SendDeviceCommandAsync(string deviceId, List<TuyaCommand> commands)
        {
            var token = await GetAccessTokenAsync();
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            
            var commandRequest = new TuyaCommandRequest { Commands = commands };
            var jsonContent = JsonSerializer.Serialize(commandRequest);
            
            var request = new HttpRequestMessage(HttpMethod.Post, $"/v1.0/devices/{deviceId}/commands")
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };
            
            AddAuthHeaders(request, token, timestamp, jsonContent);

            try
            {
                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation("Device command response: {Content}", content);
                
                var commandResponse = JsonSerializer.Deserialize<TuyaCommandResponse>(content);
                return commandResponse?.Success == true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending command to device {DeviceId}", deviceId);
                return false;
            }
        }

        // 🔑 API INTEGRATION POINT 5: Get Device Functions
        public async Task<List<TuyaDeviceFunction>> GetDeviceFunctionsAsync(string deviceId)
        {
            var token = await GetAccessTokenAsync();
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            
            var request = new HttpRequestMessage(HttpMethod.Get, $"/v1.0/devices/{deviceId}/functions");
            AddAuthHeaders(request, token, timestamp);

            try
            {
                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                
                // Parse and return device functions
                // Implementation depends on Tuya API response structure
                return new List<TuyaDeviceFunction>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting device functions for device {DeviceId}", deviceId);
                return new List<TuyaDeviceFunction>();
            }
        }

        // 🔑 API INTEGRATION POINT 6: Register Webhook
        public async Task<bool> RegisterWebhookAsync(string callbackUrl)
        {
            var token = await GetAccessTokenAsync();
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            
            var webhookRequest = new
            {
                callback_url = callbackUrl,
                event_types = new[] { "device.status.update", "device.online", "device.offline" }
            };
            
            var jsonContent = JsonSerializer.Serialize(webhookRequest);
            
            var request = new HttpRequestMessage(HttpMethod.Post, "/v1.0/iot-03/devices/status/subscribe")
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };
            
            AddAuthHeaders(request, token, timestamp, jsonContent);

            try
            {
                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation("Webhook registration response: {Content}", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering webhook");
                return false;
            }
        }

        // 🔑 API INTEGRATION POINT 7: Get Tuya Auth URL
        public async Task<string> GetTuyaAuthUrlAsync(string state)
        {
            var clientId = _configuration["TuyaApi:ClientId"];
            var redirectUri = _configuration["TuyaApi:RedirectUri"];
            var baseUrl = _configuration["TuyaApi:BaseUrl"];
            
            var authUrl = $"{baseUrl}/v1.0/oauth2/authorize" +
                         $"?client_id={clientId}" +
                         $"&response_type=code" +
                         $"&redirect_uri={Uri.EscapeDataString(redirectUri!)}" +
                         $"&state={state}" +
                         $"&scope=user:read device:read device:write";
            
            return authUrl;
        }

        // 🔑 API INTEGRATION POINT 8: Exchange Code for Token
        public async Task<TuyaTokenResponse> ExchangeCodeForTokenAsync(string code)
        {
            var clientId = _configuration["TuyaApi:ClientId"];
            var clientSecret = _configuration["TuyaApi:ClientSecret"];
            var redirectUri = _configuration["TuyaApi:RedirectUri"];
            
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            var signString = clientId + timestamp;
            var sign = ComputeHmacSha256(signString, clientSecret!);

            var tokenRequest = new
            {
                grant_type = "authorization_code",
                code = code,
                redirect_uri = redirectUri
            };
            
            var jsonContent = JsonSerializer.Serialize(tokenRequest);
            
            var request = new HttpRequestMessage(HttpMethod.Post, "/v1.0/oauth2/token")
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };
            
            request.Headers.Add("client_id", clientId);
            request.Headers.Add("sign", sign);
            request.Headers.Add("t", timestamp);
            request.Headers.Add("sign_method", "HMAC-SHA256");

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            
            return JsonSerializer.Deserialize<TuyaTokenResponse>(content) ?? new TuyaTokenResponse();
        }

        private void AddAuthHeaders(HttpRequestMessage request, string token, string timestamp, string? body = null)
        {
            var clientId = _configuration["TuyaApi:ClientId"];
            var clientSecret = _configuration["TuyaApi:ClientSecret"];
            
            var signString = clientId + token + timestamp;
            if (!string.IsNullOrEmpty(body))
                signString += body;
                
            var sign = ComputeHmacSha256(signString, clientSecret!);

            request.Headers.Add("client_id", clientId);
            request.Headers.Add("access_token", token);
            request.Headers.Add("sign", sign);
            request.Headers.Add("t", timestamp);
            request.Headers.Add("sign_method", "HMAC-SHA256");
        }

        private static string ComputeHmacSha256(string message, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            return Convert.ToHexString(hash).ToUpper();
        }
    }
}