using System.ComponentModel.DataAnnotations;

namespace DroneKurye.DTOs;

// ── Kayıt ──────────────────────────────────────────────────────────────────
public class RegisterRequest
{
    [Required(ErrorMessage = "Ad Soyad zorunludur.")]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [MinLength(8, ErrorMessage = "Şifre en az 8 karakter olmalıdır.")]
    public string Password { get; set; } = string.Empty;

    [Compare("Password", ErrorMessage = "Şifreler eşleşmiyor.")]
    public string PasswordConfirm { get; set; } = string.Empty;
}

// ── Giriş ──────────────────────────────────────────────────────────────────
public class LoginRequest
{
    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    public string Password { get; set; } = string.Empty;
}

// ── Token yenileme ──────────────────────────────────────────────────────────
public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

// ── Yanıt: Başarılı auth ────────────────────────────────────────────────────
public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public UserDto User { get; set; } = null!;
}

// ── Kullanıcı özet bilgisi ──────────────────────────────────────────────────
public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? SubscriptionPlan { get; set; }
}
