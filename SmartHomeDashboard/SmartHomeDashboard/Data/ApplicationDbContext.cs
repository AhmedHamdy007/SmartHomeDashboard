using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartHomeDashboard.Models.Entities;

namespace SmartHomeDashboard.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Device> Devices { get; set; }
        public DbSet<DeviceLog> DeviceLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AutomationRule> AutomationRules { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Device configuration
            builder.Entity<Device>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.TuyaDeviceId);
                entity.HasIndex(e => e.UserId);
                
                entity.HasOne(d => d.User)
                    .WithMany(u => u.Devices)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // DeviceLog configuration
            builder.Entity<DeviceLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.DeviceId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Timestamp);
                
                entity.HasOne(d => d.Device)
                    .WithMany(d => d.DeviceLogs)
                    .HasForeignKey(d => d.DeviceId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(d => d.User)
                    .WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Notification configuration
            builder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CreatedAt);
                
                entity.HasOne(n => n.User)
                    .WithMany(u => u.Notifications)
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // AutomationRule configuration
            builder.Entity<AutomationRule>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);
                
                entity.HasOne(a => a.User)
                    .WithMany(u => u.AutomationRules)
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}