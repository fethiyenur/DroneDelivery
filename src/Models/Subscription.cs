namespace DroneKurye.Models;

public class Subscription
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Free;
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public string? IyzicoSubscriptionId { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}

public enum SubscriptionPlan
{
    Free = 0,
    Premium = 1
}

public enum SubscriptionStatus
{
    Active = 0,
    Cancelled = 1,
    Expired = 2
}
