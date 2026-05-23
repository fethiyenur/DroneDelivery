using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using DroneKurye.Data;
using DroneKurye.Hubs;
using DroneKurye.Middleware;
using DroneKurye.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Render PORT fix
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// ── SQLite yolu
var dbPath = Path.Combine(AppContext.BaseDirectory, "dronekurye.db");
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite($"Data Source={dbPath}"));

// ── JWT
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret yapılandırılmamış!");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        opt.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(token) && path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(Policies.Register);

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddHttpClient();

builder.Services.AddSingleton<DroneSimulationService>();
builder.Services.AddSingleton<IDroneSimulationService>(sp =>
    sp.GetRequiredService<DroneSimulationService>());
builder.Services.AddHostedService(sp =>
    sp.GetRequiredService<DroneSimulationService>());

builder.Services.AddCors(opt =>
    opt.AddPolicy("Frontend", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Drone Kurye API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer", BearerFormat = "JWT",
        In = ParameterLocation.Header, Description = "Bearer {token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {{
        new OpenApiSecurityScheme { Reference = new OpenApiReference
            { Type = ReferenceType.SecurityScheme, Id = "Bearer" }},
        Array.Empty<string>()
    }});
});

var app = builder.Build();

// ── Swagger sadece Dev'de
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ── ✅ Migration HER ortamda çalışsın (kritik düzeltme)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}
// Migration'ın hemen altına ekle
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    // ── Seed: Admin kullanıcısı yoksa oluştur
    if (!db.Users.Any(u => u.Email == "admin@dronekurye.com"))
    {
        var admin = new DroneKurye.Models.User
        {
            FullName = "Admin",
            Email = "admin@dronekurye.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Role = DroneKurye.Models.Role.Admin,
            IsActive = true,
            IsOnline = false,
            LastLoginAt = DateTime.UtcNow
        };
        db.Users.Add(admin);
        db.Subscriptions.Add(new DroneKurye.Models.Subscription
        {
            User = admin,
            Plan = DroneKurye.Models.SubscriptionPlan.Free,
            Status = DroneKurye.Models.SubscriptionStatus.Active
        });
        await db.SaveChangesAsync();
    }
}

app.UseCors("Frontend");
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<DroneHub>("/hubs/drone");
app.MapGet("/ping", () => "Drone API is running");
app.MapGet("/debug-files", () =>
{
    var wwwroot = Path.Combine(AppContext.BaseDirectory, "wwwroot", "models");
    if (!Directory.Exists(wwwroot))
        return Results.Ok("wwwroot/models klasörü YOK. BaseDir: " + AppContext.BaseDirectory);
    var files = Directory.GetFiles(wwwroot);
    return Results.Ok(new { baseDir = AppContext.BaseDirectory, files = files });
});


app.Run();