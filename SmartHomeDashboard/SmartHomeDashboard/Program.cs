using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartHomeDashboard.Data;
using SmartHomeDashboard.Hubs;
using SmartHomeDashboard.Models.Entities;
using SmartHomeDashboard.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity services
builder.Services.AddIdentity<User, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
}).AddEntityFrameworkStores<ApplicationDbContext>();

// Add MVC services
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Add SignalR
builder.Services.AddSignalR();

// Add session support for OAuth state
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register custom services
builder.Services.AddHttpClient<ITuyaApiService, TuyaApiService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();

// Add logging
builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Configure routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();
app.MapHub<DeviceStatusHub>("/deviceStatusHub");

// 🔑 API INTEGRATION POINT: Register Webhook on Startup
if (app.Environment.IsProduction())
{
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var tuyaService = scope.ServiceProvider.GetRequiredService<ITuyaApiService>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            
            var webhookUrl = $"https://yourdomain.com/api/webhook/tuya/status";
            var registered = await tuyaService.RegisterWebhookAsync(webhookUrl);
            
            if (registered)
            {
                logger.LogInformation("Tuya webhook registered successfully: {WebhookUrl}", webhookUrl);
            }
            else
            {
                logger.LogWarning("Failed to register Tuya webhook: {WebhookUrl}", webhookUrl);
            }
        }
        catch (Exception ex)
        {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Error registering Tuya webhook on startup");
        }
    }
}

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
}

app.Run();