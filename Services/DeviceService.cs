using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using SmartHomeDashboard.Data;
using SmartHomeDashboard.Models.Entities;
using SmartHomeDashboard.Models.DTOs;

namespace SmartHomeDashboard.Services
{
    public class DeviceService : IDeviceService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DeviceService> _logger;

        public DeviceService(ApplicationDbContext context, ILogger<DeviceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Device>> GetUserDevicesAsync(string userId)
        {
            return await _context.Devices
                .Where(d => d.UserId == userId)
                .OrderBy(d => d.Name)
                .ToListAsync();
        }

        public async Task<Device?> GetDeviceAsync(int deviceId)
        {
            return await _context.Devices
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == deviceId);
        }

        public async Task<Device?> GetDeviceByTuyaIdAsync(string tuyaDeviceId)
        {
            return await _context.Devices
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.TuyaDeviceId == tuyaDeviceId);
        }

        public async Task<User?> GetUserWithTuyaTokenAsync(string userId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task SyncDevicesAsync(string userId, List<TuyaDevice> tuyaDevices)
        {
            try
            {
                var existingDevices = await _context.Devices
                    .Where(d => d.UserId == userId)
                    .ToListAsync();

                foreach (var tuyaDevice in tuyaDevices)
                {
                    var existingDevice = existingDevices
                        .FirstOrDefault(d => d.TuyaDeviceId == tuyaDevice.Id);

                    if (existingDevice != null)
                    {
                        // Update existing device
                        existingDevice.Name = tuyaDevice.Name;
                        existingDevice.Category = tuyaDevice.Category;
                        existingDevice.ProductId = tuyaDevice.ProductId;
                        existingDevice.ProductName = tuyaDevice.ProductName;
                        existingDevice.IsOnline = tuyaDevice.Online;
                        existingDevice.Status = JsonSerializer.Serialize(tuyaDevice.Status);
                        existingDevice.Icon = tuyaDevice.Icon;
                        existingDevice.LastUpdated = DateTime.UtcNow;
                    }
                    else
                    {
                        // Add new device
                        var newDevice = new Device
                        {
                            TuyaDeviceId = tuyaDevice.Id,
                            UserId = userId,
                            Name = tuyaDevice.Name,
                            Category = tuyaDevice.Category,
                            ProductId = tuyaDevice.ProductId,
                            ProductName = tuyaDevice.ProductName,
                            IsOnline = tuyaDevice.Online,
                            Status = JsonSerializer.Serialize(tuyaDevice.Status),
                            Icon = tuyaDevice.Icon,
                            CreatedAt = DateTime.UtcNow,
                            LastUpdated = DateTime.UtcNow
                        };

                        _context.Devices.Add(newDevice);
                    }
                }

                await _context.SaveChangesAsync();
                
                // Create notification for sync completion
                await CreateNotificationAsync(userId, "Devices Synced", 
                    $"Successfully synced {tuyaDevices.Count} devices from Tuya Cloud", "success");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing devices for user {UserId}", userId);
                throw;
            }
        }

        public async Task UpdateDeviceStatusAsync(string tuyaDeviceId, List<TuyaDeviceStatus> status)
        {
            try
            {
                var device = await GetDeviceByTuyaIdAsync(tuyaDeviceId);
                if (device != null)
                {
                    device.Status = JsonSerializer.Serialize(status);
                    device.LastUpdated = DateTime.UtcNow;
                    
                    await _context.SaveChangesAsync();
                    
                    // Log the status change
                    await LogDeviceActionAsync(tuyaDeviceId, "status_update", JsonSerializer.Serialize(status));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating device status for device {DeviceId}", tuyaDeviceId);
            }
        }

        public async Task LogDeviceActionAsync(string tuyaDeviceId, string command, string value)
        {
            try
            {
                var device = await GetDeviceByTuyaIdAsync(tuyaDeviceId);
                if (device != null)
                {
                    var log = new DeviceLog
                    {
                        DeviceId = device.Id,
                        UserId = device.UserId,
                        EventType = "manual_control",
                        Command = command,
                        Value = value,
                        Timestamp = DateTime.UtcNow
                    };

                    _context.DeviceLogs.Add(log);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging device action for device {DeviceId}", tuyaDeviceId);
            }
        }

        public async Task ProcessAutomationRulesAsync(string tuyaDeviceId, List<TuyaDeviceStatus> status)
        {
            try
            {
                var device = await GetDeviceByTuyaIdAsync(tuyaDeviceId);
                if (device == null) return;

                var automationRules = await _context.AutomationRules
                    .Where(r => r.UserId == device.UserId && r.IsActive)
                    .ToListAsync();

                foreach (var rule in automationRules)
                {
                    // Parse trigger conditions and check if they match current status
                    // This is a simplified implementation - you'd want more sophisticated rule processing
                    var triggerConditions = JsonSerializer.Deserialize<Dictionary<string, object>>(rule.TriggerConditions);
                    var actions = JsonSerializer.Deserialize<Dictionary<string, object>>(rule.Actions);

                    // Check if conditions are met
                    bool conditionsMet = CheckAutomationConditions(triggerConditions, status, device);

                    if (conditionsMet)
                    {
                        // Execute actions
                        await ExecuteAutomationActions(actions, device.UserId);
                        
                        // Log automation trigger
                        var log = new DeviceLog
                        {
                            DeviceId = device.Id,
                            UserId = device.UserId,
                            EventType = "automation_trigger",
                            EventData = JsonSerializer.Serialize(new { rule = rule.RuleName, trigger = triggerConditions }),
                            Timestamp = DateTime.UtcNow
                        };

                        _context.DeviceLogs.Add(log);
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing automation rules for device {DeviceId}", tuyaDeviceId);
            }
        }

        public async Task<List<DeviceLog>> GetDeviceLogsAsync(string userId, int? deviceId = null)
        {
            var query = _context.DeviceLogs
                .Include(l => l.Device)
                .Where(l => l.UserId == userId);

            if (deviceId.HasValue)
                query = query.Where(l => l.DeviceId == deviceId.Value);

            return await query
                .OrderByDescending(l => l.Timestamp)
                .Take(100)
                .ToListAsync();
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(string userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToListAsync();
        }

        public async Task CreateNotificationAsync(string userId, string title, string message, string type)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification for user {UserId}", userId);
            }
        }

        private bool CheckAutomationConditions(Dictionary<string, object>? conditions, List<TuyaDeviceStatus> status, Device device)
        {
            if (conditions == null) return false;

            // Simplified condition checking - implement your logic here
            // Example: Check if a specific status code matches a value
            foreach (var condition in conditions)
            {
                var statusItem = status.FirstOrDefault(s => s.Code == condition.Key);
                if (statusItem != null && statusItem.Value.ToString() == condition.Value.ToString())
                {
                    return true;
                }
            }

            return false;
        }

        private async Task ExecuteAutomationActions(Dictionary<string, object>? actions, string userId)
        {
            if (actions == null) return;

            // Simplified action execution - implement your logic here
            // Example: Send notification, control other devices, etc.
            foreach (var action in actions)
            {
                switch (action.Key)
                {
                    case "notify":
                        await CreateNotificationAsync(userId, "Automation Triggered", 
                            action.Value.ToString() ?? "Automation rule executed", "info");
                        break;
                    // Add more action types as needed
                }
            }
        }
    }
}