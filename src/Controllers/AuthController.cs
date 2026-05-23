using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DroneKurye.DTOs;
using DroneKurye.Services;
using System.Security.Claims;

namespace DroneKurye.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Yeni kullanıcı kaydı</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var (ok, error, response) = await _auth.RegisterAsync(req);
        if (!ok) return BadRequest(new { message = error });
        return Ok(response);
    }

    /// <summary>Kullanıcı girişi</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var (ok, error, response) = await _auth.LoginAsync(req);
        if (!ok) return Unauthorized(new { message = error });
        return Ok(response);
    }

    /// <summary>Access token yenileme</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest req)
    {
        var (ok, error, response) = await _auth.RefreshAsync(req.RefreshToken);
        if (!ok) return Unauthorized(new { message = error });
        return Ok(response);
    }

    /// <summary>Çıkış (refresh token iptal)</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest req)
    {
        await _auth.RevokeAsync(req.RefreshToken);
        return NoContent();
    }

    /// <summary>Giriş yapmış kullanıcının bilgilerini döner</summary>
    
}
