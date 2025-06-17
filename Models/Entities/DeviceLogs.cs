using System.ComponentModel.DataAnnotations;

namespace SmartHomeDashboard.Models.Entities
{
    public class DeviceLog
    {
        public int Id { get; set; }
        
        [Required]
        public int DeviceId { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        public string EventType { get; set; } = string.Empty; // status_change, automation_trigger, manual_control
        
        public string? EventData { get; set; } // JSON data
        public string? Command { get; set; }
        public string? Value { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public virtual Device Device { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}