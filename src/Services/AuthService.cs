using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using DroneKurye.Data;
using DroneKurye.DTOs;
using DroneKurye.Models;

namespace DroneKurye.Services;

public interface IAuthService
{
    Task<(bool Success, string? Error, AuthResponse? Response)> RegisterAsync(RegisterRequest req);
    Task<(bool Success, string? Error, AuthResponse? Response)> LoginAsync(LoginRequest req);
    Task<(bool Success, string? Error, AuthResponse? Response)> RefreshAsync(string refreshToken);
    Task<bool> RevokeAsync(string refreshToken);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokens;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, ITokenService tokens, IConfiguration config)
    {
        _db = db;
        _tokens = tokens;
        _config = config;
    }

    public async Task<(bool, string?, AuthResponse?)> RegisterAsync(RegisterRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Email == req.Email.ToLower()))
            return (false, "Bu e-posta zaten kayıtlı.", null);

        var user = new User
        {
            FullName = req.FullName.Trim(),
            Email = req.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = Role.Guest,
            IsOnline = true,
            LastLoginAt = DateTime.UtcNow
        };

        _db.Users.Add(user);

        _db.Subscriptions.Add(new Subscription
        {
            User = user,
            Plan = SubscriptionPlan.Free,
            Status = SubscriptionStatus.Active
        });

        await _db.SaveChangesAsync();
        return (true, null, await BuildAuthResponse(user));
    }

    public async Task<(bool, string?, AuthResponse?)> LoginAsync(LoginRequest req)
    {
        var user = await _db.Users
            .Include(u => u.Subscription)
            .FirstOrDefaultAsync(u => u.Email == req.Email.ToLower());

        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return (false, "E-posta veya şifre hatalı.", null);

        if (!user.IsActive)
            return (false, "Hesabınız askıya alınmış.", null);

        user.LastLoginAt = DateTime.UtcNow;
        user.IsOnline = true;
        await _db.SaveChangesAsync();

        return (true, null, await BuildAuthResponse(user));
    }

    public async Task<(bool, string?, AuthResponse?)> RefreshAsync(string refreshToken)
    {
        var stored = await _db.RefreshTokens
            .Include(r => r.User).ThenInclude(u => u.Subscription)
            .FirstOrDefaultAsync(r => r.Token == refreshToken && !r.IsRevoked);

        if (stored is null || stored.ExpiresAt < DateTime.UtcNow)
            return (false, "Geçersiz veya süresi dolmuş refresh token.", null);

        stored.IsRevoked = true;

        var newRefresh = new RefreshToken
        {
            UserId = stored.UserId,
            Token = _tokens.GenerateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(
                int.Parse(_config["Jwt:RefreshTokenDays"] ?? "30"))
        };
        _db.RefreshTokens.Add(newRefresh);
        await _db.SaveChangesAsync();

        var accessToken = _tokens.GenerateAccessToken(stored.User);
        return (true, null, new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefresh.Token,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(
                int.Parse(_config["Jwt:AccessTokenMinutes"] ?? "60")),
            User = ToDto(stored.User)
        });
    }

    public async Task<bool> RevokeAsync(string refreshToken)
    {
        var stored = await _db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == refreshToken && !r.IsRevoked);

        if (stored is null) return false;

        stored.IsRevoked = true;

        // Çıkış yapınca IsOnline = false
        stored.User.IsOnline = false;
        stored.User.LastLogoutAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<AuthResponse> BuildAuthResponse(User user)
    {
        if (user.Subscription is null)
            await _db.Entry(user).Reference(u => u.Subscription).LoadAsync();

        var refreshDays = int.Parse(_config["Jwt:RefreshTokenDays"] ?? "30");
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = _tokens.GenerateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(refreshDays)
        };
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = _tokens.GenerateAccessToken(user),
            RefreshToken = refreshToken.Token,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(
                int.Parse(_config["Jwt:AccessTokenMinutes"] ?? "60")),
            User = ToDto(user)
        };
    }

    private static UserDto ToDto(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role.ToString(),
        SubscriptionPlan = user.Subscription?.Plan.ToString()
    };
}
