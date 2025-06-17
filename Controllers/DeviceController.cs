using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SmartHomeDashboard.Hubs;
using SmartHomeDashboard.Models.DTOs;
using SmartHomeDashboard.Services;
using System.Security.Claims;

namespace SmartHomeDashboard.Controllers
{
    [Authorize]
    public class DeviceController : Controller
    {
        private readonly ITuyaApiService _tuyaApiService;
        private readonly IDeviceService _deviceService;
        private readonly IHubContext<DeviceStatusHub> _hubContext;
        private readonly ILogger<DeviceController> _logger;

        public DeviceController(
            ITuyaApiService tuyaApiService, 
            IDeviceService deviceService, 
            IHubContext<DeviceStatusHub> hubContext,
            ILogger<DeviceController> logger)
        {
            _tuyaApiService = tuyaApiService;
            _deviceService = deviceService;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var devices = await _deviceService.GetUserDevicesAsync(userId);
            return View(devices);
        }

        public async Task<IActionResult> Details(int id)
        {
            var device = await _deviceService.GetDeviceAsync(id);
            if (device == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (device.UserId != userId)
                return Forbid();

            // Get current device status from Tuya
            var status = await _tuyaApiService.GetDeviceStatusAsync(device.TuyaDeviceId);
            ViewBag.CurrentStatus = status;

            // Get device logs
            var logs = await _deviceService.GetDeviceLogsAsync(userId, device.Id);
            ViewBag.DeviceLogs = logs;

            return View(device);
        }

        // 🔑 API INTEGRATION POINT: Sync Devices from Tuya
        [HttpPost]
        public async Task<IActionResult> SyncDevices()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var user = await _deviceService.GetUserWithTuyaTokenAsync(userId);
                
                if (string.IsNullOrEmpty(user?.TuyaUserId))
                {
                    return Json(new { success = false, message = "Tuya account not linked. Please link your Tuya account first." });
                }

                var tuyaDevices = await _tuyaApiService.GetUserDevicesAsync(user.TuyaUserId);
                await _deviceService.SyncDevicesAsync(userId, tuyaDevices);

                return Json(new { success = true, message = $"Successfully synced {tuyaDevices.Count} devices" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing devices");
                return Json(new { success = false, message = "Failed to sync devices. Please try again." });
            }
        }

        // 🔑 API INTEGRATION POINT: Control Device
        [HttpPost]
        public async Task<IActionResult> ControlDevice([FromBody] DeviceControlRequest request)
        {
            try
            {
                var device = await _deviceService.GetDeviceByTuyaIdAsync(request.DeviceId);
                if (device == null)
                    return Json(new { success = false, message = "Device not found" });

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                if (device.UserId != userId)
                    return Json(new { success = false, message = "Unauthorized" });

                var commands = new List<TuyaCommand>
                {
                    new TuyaCommand { Code = request.Command, Value = request.Value }
                };

                var success = await _tuyaApiService.SendDeviceCommandAsync(request.DeviceId, commands);
                
                if (success)
                {
                    // Notify all connected clients via SignalR
                    await _hubContext.Clients.All.SendAsync("DeviceStatusChanged", 
                        request.DeviceId, request.Command, request.Value);
                    
                    // Log the action
                    await _deviceService.LogDeviceActionAsync(request.DeviceId, request.Command, request.Value?.ToString() ?? "");
                    
                    return Json(new { success = true, message = "Device controlled successfully" });
                }

                return Json(new { success = false, message = "Failed to control device" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error controlling device {DeviceId}", request.DeviceId);
                return Json(new { success = false, message = "An error occurred while controlling the device" });
            }
        }

        // 🔑 API INTEGRATION POINT: Get Device Status
        [HttpGet]
        public async Task<IActionResult> GetDeviceStatus(string deviceId)
        {
            try
            {
                var device = await _deviceService.GetDeviceByTuyaIdAsync(deviceId);
                if (device == null)
                    return Json(new { success = false, message = "Device not found" });

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                if (device.UserId != userId)
                    return Json(new { success = false, message = "Unauthorized" });

                var status = await _tuyaApiService.GetDeviceStatusAsync(deviceId);
                return Json(new { success = true, status = status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting device status for {DeviceId}", deviceId);
                return Json(new { success = false, message = "Failed to get device status" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RefreshDeviceStatus(string deviceId)
        {
            try
            {
                var status = await _tuyaApiService.GetDeviceStatusAsync(deviceId);
                await _deviceService.UpdateDeviceStatusAsync(deviceId, status);
                
                // Notify clients of status update
                await _hubContext.Clients.All.SendAsync("DeviceStatusUpdated", deviceId, status);
                
                return Json(new { success = true, status = status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing device status for {DeviceId}", deviceId);
                return Json(new { success = false, message = "Failed to refresh device status" });
            }
        }
    }

    public class DeviceControlRequest
    {
        public string DeviceId { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public object? Value { get; set; }
    }
}