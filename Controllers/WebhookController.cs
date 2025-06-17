using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SmartHomeDashboard.Hubs;
using SmartHomeDashboard.Models.DTOs;
using SmartHomeDashboard.Services;

namespace SmartHomeDashboard.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebhookController : ControllerBase
    {
        private readonly IDeviceService _deviceService;
        private readonly IHubContext<DeviceStatusHub> _hubContext;
        private readonly ILogger<WebhookController> _logger;

        public WebhookController(
            IDeviceService deviceService, 
            IHubContext<DeviceStatusHub> hubContext,
            ILogger<WebhookController> logger)
        {
            _deviceService = deviceService;
            _hubContext = hubContext;
            _logger = logger;
        }

        // 🔑 API INTEGRATION POINT: Receive Tuya Webhooks
        [HttpPost("tuya/status")]
        public async Task<IActionResult> TuyaDeviceStatus([FromBody] TuyaWebhookPayload payload)
        {
            try
            {
                _logger.LogInformation("Received Tuya webhook for device {DeviceId}: {Payload}", 
                    payload.DeviceId, System.Text.Json.JsonSerializer.Serialize(payload));

                // Update device status in database
                await _deviceService.UpdateDeviceStatusAsync(payload.DeviceId, payload.Status);
                
                // Notify connected clients via SignalR
                await _hubContext.Clients.All.SendAsync("DeviceStatusUpdated", payload.DeviceId, payload.Status);
                
                // Process automation rules
                await _deviceService.ProcessAutomationRulesAsync(payload.DeviceId, payload.Status);

                return Ok(new { success = true, message = "Webhook processed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Tuya webhook for device {DeviceId}", payload.DeviceId);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // Health check endpoint for webhook registration
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }
    }
}