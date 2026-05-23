# Drone Kurye — Aşama 1: Proje Altyapısı & Auth

## Kurulum Adımları

### 1. Gereksinimler
- .NET 8 SDK
- SQL Server (yerel) veya SQL Server Express
- Visual Studio 2022 / VS Code + C# Dev Kit

### 2. Paketi yükle
```bash
cd DroneKurye
dotnet restore
```

### 3. appsettings.json'u düzenle
```json
"ConnectionStrings": {
  "Default": "Server=localhost;Database=DroneKurye;..."
},
"Jwt": {
  "Secret": "BURAYA_EN_AZ_32_KARAKTER_GIZLI_ANAHTAR_YAZ"
}
```
⚠️ JWT Secret'ı güçlü bir değerle değiştir (örn. `openssl rand -base64 32`).

### 4. Migration oluştur & veritabanını kur
```bash
dotnet ef migrations add InitialCreate --project DroneKurye.csproj --output-dir src/Data/Migrations
dotnet ef database update
```

### 5. Uygulamayı başlat
```bash
dotnet run
```
Swagger UI: https://localhost:5001/swagger

---

## Hazır Endpoint'ler

| Metot | URL | Açıklama |
|-------|-----|----------|
| POST | /api/auth/register | Kayıt |
| POST | /api/auth/login | Giriş → AccessToken + RefreshToken |
| POST | /api/auth/refresh | Token yenileme |
| POST | /api/auth/logout | Çıkış (token iptal) |
| GET  | /api/auth/me | Giriş yapan kullanıcı bilgisi |

---

## Varsayılan Admin Hesabı
- **E-posta:** admin@dronekurye.com
- **Şifre:** Admin123!
- ⚠️ Canlıya almadan önce şifreyi değiştir!

---

## Veritabanı Şeması (Oluşturulan Tablolar)

- `Users` — Kullanıcılar (rol: Guest / Subscriber / Admin)
- `RefreshTokens` — JWT refresh token yönetimi
- `Subscriptions` — Free / Premium abonelik
- `DroneDataHistory` — Telemetri geçmiş kayıtları
- `DeliveryOrders` — Teslimat siparişleri
- `Advertisements` — Reklam yönetimi
- `Notifications` — Bildirimler

---

## Sonraki Aşama (Aşama 2)
- `SignalR DroneHub` — gerçek zamanlı telemetri yayını
- Simülasyon servisi
- Dashboard UI
