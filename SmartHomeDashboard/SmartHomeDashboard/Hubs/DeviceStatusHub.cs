using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SmartHomeDashboard.Hubs
{
    [Authorize]
    public class DeviceStatusHub : Hub
    {
        public async Task JoinDeviceGroup(string deviceId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"device_{deviceId}");
        }

        public async Task LeaveDeviceGroup(string deviceId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"device_{deviceId}");
        }

        public async Task JoinUserGroup(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        public override async Task OnConnectedAsync()
        {
            // Add user to their personal group for targeted notifications
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            }
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Cleanup is handled automatically by SignalR
            await base.OnDisconnectedAsync(exception);
        }
    }
}