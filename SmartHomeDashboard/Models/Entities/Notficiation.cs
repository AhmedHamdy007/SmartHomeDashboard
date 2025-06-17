using System.ComponentModel.DataAnnotations;

namespace SmartHomeDashboard.Models.Entities
{
    public class Notification
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Message { get; set; } = string.Empty;
        
        [Required]
        public string Type { get; set; } = string.Empty; // info, warning, error, success
        
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public virtual User User { get; set; } = null!;
    }
}