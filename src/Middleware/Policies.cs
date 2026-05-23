using Microsoft.AspNetCore.Authorization;

namespace DroneKurye.Middleware;

/// <summary>
/// Kullanım örnekleri:
///   [Authorize(Policy = Policies.SubscriberOrAbove)]
///   [Authorize(Policy = Policies.AdminOnly)]
/// </summary>
public static class Policies
{
    public const string AdminOnly = "AdminOnly";
    public const string SubscriberOrAbove = "SubscriberOrAbove";

    public static void Register(AuthorizationOptions options)
    {
        options.AddPolicy(AdminOnly, policy =>
            policy.RequireRole("Admin"));

        options.AddPolicy(SubscriberOrAbove, policy =>
            policy.RequireRole("Subscriber", "Admin"));
    }
}
