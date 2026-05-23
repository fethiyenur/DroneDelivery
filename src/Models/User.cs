using System.ComponentModel.DataAnnotations;

namespace DroneKurye.Models;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public Role Role { get; set; } = Role.Guest;

    public bool IsActive { get; set; } = true;

    public bool IsOnline { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    public DateTime? LastLogoutAt { get; set; }

    // Navigation
    public Subscription? Subscription { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

public enum Role
{
    Guest = 0,
    Subscriber = 1,
    Admin = 2
}
