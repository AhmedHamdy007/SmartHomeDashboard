using System.ComponentModel.DataAnnotations;

namespace SmartHomeDashboard.Models.Entities
{
    public class Device
    {
        public int Id { get; set; }
        
        [Required]
        public string TuyaDeviceId { get; set; } = string.Empty;
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string Category { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public string Status { get; set; } = "{}"; // JSON string for device status
        public string? Location { get; set; }
        public string? Icon { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        
        public virtual User User { get; set; } = null!;
        public virtual ICollection<DeviceLog> DeviceLogs { get; set; } = new List<DeviceLog>();
    }
}