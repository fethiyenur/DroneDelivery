# ---------- BUILD STAGE ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# csproj kopyala ve restore yap
COPY *.csproj ./
RUN dotnet restore

# tüm projeyi kopyala
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# ---------- RUNTIME STAGE ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .

# ── ⚠️ Render Port Fix: Sabit portu kaldırıyoruz, Program.cs dinamik yönetecek
EXPOSE 8080

# DLL adı burada senin proje adına göre olmalı
ENTRYPOINT ["dotnet", "DroneKurye.dll"]