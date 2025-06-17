using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartHomeDashboard.Models.Entities;
using SmartHomeDashboard.Services;
using System.Security.Claims;

namespace SmartHomeDashboard.Controllers
{
    public class AuthController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ITuyaApiService _tuyaApiService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ITuyaApiService tuyaApiService,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tuyaApiService = tuyaApiService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    return RedirectToLocal(returnUrl);
                }
                
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var user = new User 
                { 
                    UserName = model.Email, 
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName
                };
                
                var result = await _userManager.CreateAsync(user, model.Password);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToLocal(returnUrl);
                }
                
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        // 🔑 API INTEGRATION POINT: Tuya OAuth Integration - FIXED
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> LinkTuyaAccount()
        {
            var state = Guid.NewGuid().ToString();
            HttpContext.Session.SetString("TuyaOAuthState", state);
            
            _logger.LogInformation("Initiating Tuya OAuth with state: {State}", state);
            
            var authUrl = await _tuyaApiService.GetTuyaAuthUrlAsync(state);
            
            _logger.LogInformation("Redirecting to Tuya auth URL: {AuthUrl}", authUrl);
            
            return Redirect(authUrl);
        }

        // FIXED: Changed route to match the configured redirect URI
        [HttpGet]
        [Route("Auth/Tuya/Callback")]
        [Authorize]
        public async Task<IActionResult> TuyaCallback(string code, string state)
        {
            _logger.LogInformation("Tuya callback received with code: {Code}, state: {State}", 
                code?.Substring(0, Math.Min(code.Length, 10)) + "...", state);

            try
            {
                // Validate state parameter
                var sessionState = HttpContext.Session.GetString("TuyaOAuthState");
                if (string.IsNullOrEmpty(sessionState) || sessionState != state)
                {
                    _logger.LogWarning("Invalid state parameter. Session: {SessionState}, Received: {ReceivedState}", 
                        sessionState, state);
                    TempData["Error"] = "Invalid authorization state. Please try again.";
                    return RedirectToAction("Index", "Home");
                }

                // Check if code is present
                if (string.IsNullOrEmpty(code))
                {
                    _logger.LogWarning("No authorization code received from Tuya");
                    TempData["Error"] = "No authorization code received. Please try again.";
                    return RedirectToAction("Index", "Home");
                }

                // Exchange code for token
                var tokenResponse = await _tuyaApiService.ExchangeCodeForTokenAsync(code);
                
                if (tokenResponse.Success && tokenResponse.Result != null)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                    var user = await _userManager.FindByIdAsync(userId);
                    
                    if (user != null)
                    {
                        // Store Tuya user information
                        user.TuyaUserId = tokenResponse.Result.Uid;
                        user.TuyaAccessToken = tokenResponse.Result.AccessToken;
                        user.TuyaTokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.Result.ExpireTime);
                        user.TuyaRefreshToken = tokenResponse.Result.RefreshToken;
                        
                        var updateResult = await _userManager.UpdateAsync(user);
                        
                        if (updateResult.Succeeded)
                        {
                            _logger.LogInformation("Successfully linked Tuya account for user {UserId}", userId);
                            TempData["Success"] = "Tuya account linked successfully!";
                        }
                        else
                        {
                            _logger.LogError("Failed to update user with Tuya information: {Errors}", 
                                string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                            TempData["Error"] = "Failed to save Tuya account information.";
                        }
                        
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        _logger.LogError("User not found with ID: {UserId}", userId);
                        TempData["Error"] = "User account not found.";
                    }
                }
                else
                {
                    _logger.LogError("Token exchange failed: {Message}", tokenResponse.Message);
                    TempData["Error"] = $"Failed to link Tuya account: {tokenResponse.Message}";
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Tuya OAuth callback");
                TempData["Error"] = "An error occurred while linking your Tuya account. Please try again.";
                return RedirectToAction("Index", "Home");
            }
            finally
            {
                // Clean up session state
                HttpContext.Session.Remove("TuyaOAuthState");
            }
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }
    }

    public class LoginViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}