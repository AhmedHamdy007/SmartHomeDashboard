using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartHomeDashboard.Models.Entities;
using SmartHomeDashboard.Services;
using System.Security.Claims;

namespace SmartHomeDashboard.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IDeviceService _deviceService;
        private readonly ITuyaApiService _tuyaApiService;
        private readonly UserManager<User> _userManager;

        public HomeController(IDeviceService deviceService, ITuyaApiService tuyaApiService, UserManager<User> userManager)
        {
            _deviceService = deviceService;
            _tuyaApiService = tuyaApiService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // Get user info to check Tuya linking
            var user = await _userManager.FindByIdAsync(userId);
            ViewBag.IsTuyaLinked = !string.IsNullOrEmpty(user?.TuyaUserId);

            var devices = await _deviceService.GetUserDevicesAsync(userId);
            var notifications = await _deviceService.GetUserNotificationsAsync(userId);
            var recentLogs = await _deviceService.GetDeviceLogsAsync(userId);

            ViewBag.Devices = devices;
            ViewBag.Notifications = notifications.Take(5).ToList();
            ViewBag.RecentLogs = recentLogs.Take(10).ToList();
            ViewBag.OnlineDevices = devices.Count(d => d.IsOnline);
            ViewBag.TotalDevices = devices.Count;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}