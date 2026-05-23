using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DroneKurye.Data;
using DroneKurye.Middleware;
using System.Security.Claims;

namespace DroneKurye.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DroneController : ControllerBase
{
    private readonly AppDbContext _db;
    public DroneController(AppDbContext db) => _db = db;

    /// <summary>Geçmiş telemetri — Guest: 20, Subscriber/Admin: 200 kayıt</summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int count = 100, [FromQuery] string? metric = null)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        var maxCount = role == "Guest" ? 20 : 200;
        count = Math.Min(count, maxCount);

        var query = _db.DroneDataHistory
            .OrderByDescending(d => d.RecordedAt)
            .Take(count);

        var data = await query.Select(d => new {
            d.RecordedAt,
            d.Latitude, d.Longitude, d.Altitude,
            d.Speed, d.BatteryPercent, d.SignalStrength,
            d.Roll, d.Pitch, d.Yaw,
            d.FlightMode, d.ObstacleDetected,
            d.WindLevel, d.RtkStatus
        }).ToListAsync();

        return Ok(data.OrderBy(d => d.RecordedAt));
    }

    /// <summary>Son telemetri kaydı</summary>
    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest()
    {
        var latest = await _db.DroneDataHistory
            .OrderByDescending(d => d.RecordedAt)
            .FirstOrDefaultAsync();
        if (latest is null) return NotFound(new { message = "Henüz veri yok." });
        return Ok(latest);
    }

    /// <summary>Geçmiş özet istatistikler</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetHistoryStats()
    {
        var total = await _db.DroneDataHistory.CountAsync();
        if (total == 0) return Ok(new { total = 0 });

        var data = await _db.DroneDataHistory.ToListAsync();
        return Ok(new {
            total,
            avgBattery    = Math.Round(data.Average(d => d.BatteryPercent), 1),
            avgSpeed      = Math.Round(data.Average(d => d.Speed), 1),
            avgAltitude   = Math.Round(data.Average(d => d.Altitude), 1),
            maxSpeed      = Math.Round(data.Max(d => d.Speed), 1),
            maxAltitude   = Math.Round(data.Max(d => d.Altitude), 1),
            minBattery    = data.Min(d => d.BatteryPercent),
            obstacleCount = data.Count(d => d.ObstacleDetected),
            firstRecord   = data.Min(d => d.RecordedAt),
            lastRecord    = data.Max(d => d.RecordedAt)
        });
    }
}
