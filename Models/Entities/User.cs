using Microsoft.AspNetCore.Identity;

namespace SmartHomeDashboard.Models.Entities
{
    public class User : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? TuyaUserId { get; set; }
        public string? TuyaAccessToken { get; set; }
        public DateTime? TuyaTokenExpiry { get; set; }
        public string? TuyaRefreshToken { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public virtual ICollection<Device> Devices { get; set; } = new List<Device>();
        public virtual ICollection<AutomationRule> AutomationRules { get; set; } = new List<AutomationRule>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}