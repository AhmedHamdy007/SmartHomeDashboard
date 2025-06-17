using System.ComponentModel.DataAnnotations;

namespace SmartHomeDashboard.Models.Entities
{
    public class AutomationRule
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        public string RuleName { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        [Required]
        public string TriggerConditions { get; set; } = string.Empty; // JSON: conditions that trigger the rule
        
        [Required]
        public string Actions { get; set; } = string.Empty; // JSON: actions to perform
        
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public virtual User User { get; set; } = null!;
    }
}