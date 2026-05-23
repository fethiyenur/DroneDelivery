using Microsoft.EntityFrameworkCore;
using DroneKurye.Models;

namespace DroneKurye.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<DroneDataHistory> DroneDataHistory => Set<DroneDataHistory>();
    public DbSet<DeliveryOrder> DeliveryOrders => Set<DeliveryOrder>();
    public DbSet<Advertisement> Advertisements => Set<Advertisement>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // User
        mb.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasConversion<string>();
        });

        // RefreshToken → User (1:N, cascade delete)
        mb.Entity<RefreshToken>(e =>
        {
            e.HasOne(r => r.User)
             .WithMany(u => u.RefreshTokens)
             .HasForeignKey(r => r.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Subscription → User (1:1, cascade delete)
        mb.Entity<Subscription>(e =>
        {
            e.HasOne(s => s.User)
             .WithOne(u => u.Subscription)
             .HasForeignKey<Subscription>(s => s.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.Property(s => s.Plan).HasConversion<string>();
            e.Property(s => s.Status).HasConversion<string>();
        });

        // DeliveryOrder
        mb.Entity<DeliveryOrder>(e =>
        {
            e.Property(d => d.Status).HasConversion<string>();
        });

        // Advertisement
        mb.Entity<Advertisement>(e =>
        {
            e.Property(a => a.Placement).HasConversion<string>();
        });

        // Notification → User (optional FK)
        mb.Entity<Notification>(e =>
        {
            e.HasOne(n => n.User)
             .WithMany()
             .HasForeignKey(n => n.UserId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);

            e.Property(n => n.Type).HasConversion<string>();
        });

        // MotorRpms: int[] → JSON column
        mb.Entity<DroneDataHistory>(e =>
        {
            e.Property(d => d.MotorRpms)
             .HasConversion(
                v => string.Join(",", v),
                v => v.Split(",", StringSplitOptions.None).Select(int.Parse).ToArray()
             );
        });

        // Seed: Admin kullanıcı
        mb.Entity<User>().HasData(new User
        {
            Id = 1,
            FullName = "Sistem Yöneticisi",
            Email = "admin@dronekurye.com",
            // BCrypt hash: "Admin123!" — üretimde değiştir!
            PasswordHash = "$2a$11$VPfnlTv0e0FzntbQxlXR7uGt3THRP1vPXbwbI0Mj18JJ31kQaVBlq",
            Role = Role.Admin,
            IsActive = true,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
