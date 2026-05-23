using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DroneKurye.Data;
using DroneKurye.Middleware;
using DroneKurye.Models;

namespace DroneKurye.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminController(AppDbContext db) => _db = db;

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _db.Users
            .Include(u => u.Subscription)
            .OrderBy(u => u.Id)
            .Select(u => new
            {
                u.Id, u.FullName, u.Email,
                Role = u.Role.ToString(),
                u.IsActive, u.IsOnline,
                u.CreatedAt, u.LastLoginAt, u.LastLogoutAt,
                SubscriptionPlan = u.Subscription != null ? u.Subscription.Plan.ToString() : "Free",
                SubscriptionStatus = u.Subscription != null ? u.Subscription.Status.ToString() : "-"
            })
            .ToListAsync();
        return Ok(users);
    }

    [HttpPut("users/{id}/role")]
    public async Task<IActionResult> ChangeRole(int id, [FromBody] ChangeRoleRequest req)
    {
        var user = await _db.Users.Include(u => u.Subscription).FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound(new { message = "Kullanıcı bulunamadı." });

        if (!Enum.TryParse<Role>(req.Role, true, out var newRole))
            return BadRequest(new { message = "Geçersiz rol. (Guest, Subscriber, Admin)" });

        user.Role = newRole;

        if (newRole == Role.Subscriber && user.Subscription != null)
            user.Subscription.Plan = SubscriptionPlan.Premium;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Rol güncellendi.", userId = id, newRole = newRole.ToString() });
    }

    [HttpPut("users/{id}/toggle-active")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound(new { message = "Kullanıcı bulunamadı." });
        if (user.Role == Role.Admin) return BadRequest(new { message = "Admin hesabı devre dışı bırakılamaz." });

        user.IsActive = !user.IsActive;
        if (!user.IsActive) user.IsOnline = false; // Askıya alınınca çevrimdışı yap
        await _db.SaveChangesAsync();
        return Ok(new { message = user.IsActive ? "Hesap aktif edildi." : "Hesap askıya alındı.", isActive = user.IsActive });
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var totalUsers   = await _db.Users.CountAsync();
        var activeUsers  = await _db.Users.CountAsync(u => u.IsActive);
        var onlineUsers  = await _db.Users.CountAsync(u => u.IsOnline);
        var premiumUsers = await _db.Subscriptions.CountAsync(s => s.Plan == SubscriptionPlan.Premium && s.Status == SubscriptionStatus.Active);
        var totalRecords = await _db.DroneDataHistory.CountAsync();

        return Ok(new { totalUsers, activeUsers, onlineUsers, premiumUsers, totalRecords });
    }
}

public class ChangeRoleRequest
{
    public string Role { get; set; } = string.Empty;
}
