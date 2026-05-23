using Microsoft.AspNetCore.SignalR;
using DroneKurye.Hubs;
using DroneKurye.Models;
using DroneKurye.Data;
using System.Text.Json;

namespace DroneKurye.Services;

public interface IDroneSimulationService
{
    bool IsRunning { get; }
    void Start();
    void Stop();
}

public class DroneSimulationService : BackgroundService, IDroneSimulationService
{
    private readonly IHubContext<DroneHub> _hub;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DroneSimulationService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    private bool _isRunning = true;
    public bool IsRunning => _isRunning;

    private readonly Random _rng = new();
    private int _step = 0;
    private int _battery = 100;

    private const string ORS_KEY = "eyJvcmciOiI1YjNjZTM1OTc4NTExMTAwMDFjZjYyNDgiLCJpZCI6IjM2M2Q0MWJkYzlhMjQ0MGQ4MWZiNzA0NGJmZDBjZmFjIiwiaCI6Im11cm11cjY0In0=";

    private record DeliveryPoint(string Name, double BaseLat, double BaseLng, double DestLat, double DestLng);

    private readonly DeliveryPoint[] _deliveries = new[]
    {
        new DeliveryPoint("Gokçekaya Koyü",  39.8380, 30.4120, 39.8620, 30.3850),
        new DeliveryPoint("Turkmen Yaylasi", 39.8100, 30.3800, 39.8450, 30.3450),
        new DeliveryPoint("Orman Gozetleme", 39.7600, 30.3900, 39.7900, 30.3500),
    };

    private int _deliveryIdx = 0;
    private string _phase = "loading";

    private double _startLat = 39.7767, _startLng = 30.5206;
    private double _vehicleLat, _vehicleLng;
    private List<double[]> _vehicleRoadPath = new();
    private int _vehiclePathIdx = 0;

    private double _droneLat, _droneLng;
    private double _droneAlt = 5.0;
    private double _droneSpeed = 0;
    private string _flightMode = "Normal";

    public DroneSimulationService(
        IHubContext<DroneHub> hub,
        IServiceScopeFactory scopeFactory,
        ILogger<DroneSimulationService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _hub = hub;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _vehicleLat = _startLat;
        _vehicleLng = _startLng;
        _droneLat   = _startLat;
        _droneLng   = _startLng;
    }

    public void Start() { _isRunning = true;  _logger.LogInformation("Simülasyon başlatıldı."); }
    public void Stop()  { _isRunning = false; _logger.LogInformation("Simülasyon durduruldu."); }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // İlk teslimat rotasını yükle
        _phase = "loading";
        await LoadRoadPathAsync(_startLat, _startLng, _deliveries[0].BaseLat, _deliveries[0].BaseLng);
        _vehiclePathIdx = 0;
        _phase = "to_base";

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_isRunning)
            {
                var telemetry = GenerateTelemetry();
                await _hub.Clients.Group("DroneWatchers")
                    .SendAsync("TelemetryUpdate", telemetry, stoppingToken);

                if (_step % 10 == 0)
                    await SaveToDatabase(telemetry);
            }
            await Task.Delay(500, stoppingToken);
        }
    }

    private async Task LoadRoadPathAsync(double fromLat, double fromLng, double toLat, double toLng)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", ORS_KEY);

            var body = JsonSerializer.Serialize(new
            {
                coordinates = new[] { new[] { fromLng, fromLat }, new[] { toLng, toLat } }
            });

            var res = await client.PostAsync(
                "https://api.openrouteservice.org/v2/directions/driving-car/geojson",
                new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

            if (!res.IsSuccessStatusCode)
                throw new Exception("ORS HTTP " + res.StatusCode);

            var json = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var coords = doc.RootElement
                .GetProperty("features")[0]
                .GetProperty("geometry")
                .GetProperty("coordinates")
                .EnumerateArray()
                .Select(c => new double[] { c[1].GetDouble(), c[0].GetDouble() })
                .ToList();

            _vehicleRoadPath = coords;
            _logger.LogInformation("ORS rotası yüklendi: {count} nokta", coords.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("ORS rota alınamadı, düz çizgi: {msg}", ex.Message);
            _vehicleRoadPath = Enumerable.Range(0, 60)
                .Select(i => new double[]
                {
                    fromLat + (toLat - fromLat) * i / 59.0,
                    fromLng + (toLng - fromLng) * i / 59.0
                }).ToList();
        }
    }

    private TelemetryData GenerateTelemetry()
    {
        _step++;
        _battery = Math.Max(0, _battery - (_step % 80 == 0 ? 1 : 0));

        var del = _deliveryIdx < _deliveries.Length ? _deliveries[_deliveryIdx] : null;

        if (del != null)
        {
            switch (_phase)
            {
                case "to_base":
                    // Araç ORS yol noktaları üzerinde ilerliyor, drone araçla
                    if (_vehicleRoadPath.Count > 0 && _vehiclePathIdx < _vehicleRoadPath.Count)
                    {
                        var target = _vehicleRoadPath[_vehiclePathIdx];
                        _vehicleLat = target[0];
                        _vehicleLng = target[1];
                        _vehiclePathIdx++;
                    }

                    _droneLat   = _vehicleLat;
                    _droneLng   = _vehicleLng;
                    _droneAlt   = 5.0;
                    _droneSpeed = 12 + _rng.NextDouble() * 3;

                    // Rota bitti veya baz noktaya yaklaştı
                    if (_vehiclePathIdx >= _vehicleRoadPath.Count ||
                        Distance(_vehicleLat, _vehicleLng, del.BaseLat, del.BaseLng) < 30)
                    {
                        _vehicleLat = del.BaseLat;
                        _vehicleLng = del.BaseLng;
                        _droneLat   = del.BaseLat;
                        _droneLng   = del.BaseLng;
                        _phase = "to_dest";
                        _logger.LogInformation("{0}: Araç baz noktada, drone havalanıyor.", del.Name);
                    }
                    break;

                case "to_dest":
                    // Araç sabit, drone yolsuz arazide hedefe uçuyor
                    _droneAlt   = Math.Min(150, _droneAlt + 3);
                    _droneSpeed = 15 + _rng.NextDouble() * 5;
                    MoveToward(ref _droneLat, ref _droneLng, del.DestLat, del.DestLng, 0.0003);

                    if (Distance(_droneLat, _droneLng, del.DestLat, del.DestLng) < 1)
                    {
                        _droneLat = del.DestLat;
                        _droneLng = del.DestLng;
                        _phase = "returning";
                        _logger.LogInformation("{0}: Teslim edildi, drone geri dönüyor.", del.Name);
                    }
                    break;

                case "returning":
                    // Drone araça geri dönüyor
                    _droneAlt   = Math.Max(5, _droneAlt - 2);
                    _droneSpeed = 12 + _rng.NextDouble() * 3;
                    MoveToward(ref _droneLat, ref _droneLng, _vehicleLat, _vehicleLng, 0.0004);

                    if (Distance(_droneLat, _droneLng, _vehicleLat, _vehicleLng) < 10)
                    {
                        _droneLat = _vehicleLat;
                        _droneLng = _vehicleLng;
                        _droneAlt = 5;
                        _deliveryIdx++;

                        if (_deliveryIdx < _deliveries.Length)
                        {
                            var prev = _deliveries[_deliveryIdx - 1];
                            var next = _deliveries[_deliveryIdx];
                            _ = Task.Run(async () =>
                            {
                                _phase = "loading";
                                await LoadRoadPathAsync(prev.BaseLat, prev.BaseLng, next.BaseLat, next.BaseLng);
                                _vehiclePathIdx = 0;
                                _phase = "to_base";
                                _logger.LogInformation("{0}: Yeni rota yüklendi, hareket başlıyor.", next.Name);
                            });
                        }
                        else
                        {
                            // Tüm teslimatlar bitti, başa dön
                            _deliveryIdx = 0;
                            _vehicleLat  = _startLat;
                            _vehicleLng  = _startLng;
                            _droneLat    = _startLat;
                            _droneLng    = _startLng;
                            _battery     = 100;
                            _ = Task.Run(async () =>
                            {
                                _phase = "loading";
                                await LoadRoadPathAsync(_startLat, _startLng, _deliveries[0].BaseLat, _deliveries[0].BaseLng);
                                _vehiclePathIdx = 0;
                                _phase = "to_base";
                                _logger.LogInformation("Tüm teslimatlar bitti, döngü baştan başlıyor.");
                            });
                        }
                    }
                    break;

                case "loading":
                    // ORS rotası yükleniyor, bekle
                    _droneSpeed = 0;
                    break;
            }
        }

        if      (_battery < 10) _flightMode = "Failsafe";
        else if (_battery < 20) _flightMode = "ReturnToHome";
        else                    _flightMode = "Normal";

        return new TelemetryData
        {
            Timestamp  = DateTime.UtcNow,
            Latitude   = Math.Round(_droneLat, 6),
            Longitude  = Math.Round(_droneLng, 6),
            Altitude   = Math.Round(_droneAlt, 1),
            Speed      = Math.Round(_droneSpeed, 1),
            BatteryPercent   = _battery,
            SignalStrength   = _rng.Next(65, 100),
            RtkStatus        = _step % 30 < 25 ? "Fix" : "Float",
            BaseStationConnected  = true,
            BaseStationDistance   = Math.Round(_rng.NextDouble() * 500 + 100, 1),
            AccelerationX = Math.Round(_rng.NextDouble() * 2 - 1, 2),
            AccelerationY = Math.Round(_rng.NextDouble() * 2 - 1, 2),
            AccelerationZ = Math.Round(9.8 + _rng.NextDouble() * 0.4 - 0.2, 2),
            GyroscopeX    = Math.Round(_rng.NextDouble() * 0.1, 3),
            GyroscopeY    = Math.Round(_rng.NextDouble() * 0.1, 3),
            GyroscopeZ    = Math.Round(_rng.NextDouble() * 0.1, 3),
            Roll          = Math.Round(_rng.NextDouble() * 8 - 4, 1),
            Pitch         = Math.Round(_rng.NextDouble() * 8 - 4, 1),
            Yaw           = Math.Round(_rng.NextDouble() * 360, 1),
            GpsConnected     = true,
            ObstacleDetected = _rng.Next(0, 40) == 0,
            WindLevel        = _rng.Next(0, 4),
            CameraOk         = true,
            MotorsOk         = true,
            MotorRpms        = new[] { _rng.Next(3500,5000), _rng.Next(3500,5000), _rng.Next(3500,5000), _rng.Next(3500,5000) },
            FlightMode       = _flightMode,
            HmsMessage       = _battery < 20 ? "Dusuk batarya uyarisi" : null,
            VehicleLat       = Math.Round(_vehicleLat, 6),
            VehicleLng       = Math.Round(_vehicleLng, 6)
        };
    }

    private static void MoveToward(ref double lat, ref double lng, double tLat, double tLng, double step)
    {
        var dLat = tLat - lat; var dLng = tLng - lng;
        var dist = Math.Sqrt(dLat * dLat + dLng * dLng);
        if (dist < step) { lat = tLat; lng = tLng; return; }
        lat += dLat / dist * step;
        lng += dLng / dist * step;
    }

    private static double Distance(double lat1, double lng1, double lat2, double lng2)
    {
        var dLat = lat2 - lat1; var dLng = lng2 - lng1;
        return Math.Sqrt(dLat * dLat + dLng * dLng) * 111000;
    }

    private async Task SaveToDatabase(TelemetryData t)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.DroneDataHistory.Add(new DroneDataHistory
            {
                RecordedAt = t.Timestamp,
                Latitude = t.Latitude, Longitude = t.Longitude, Altitude = t.Altitude,
                Speed = t.Speed, BatteryPercent = t.BatteryPercent, SignalStrength = t.SignalStrength,
                RtkStatus = t.RtkStatus, BaseStationConnected = t.BaseStationConnected,
                BaseStationDistance = t.BaseStationDistance,
                AccelerationX = t.AccelerationX, AccelerationY = t.AccelerationY, AccelerationZ = t.AccelerationZ,
                GyroscopeX = t.GyroscopeX, GyroscopeY = t.GyroscopeY, GyroscopeZ = t.GyroscopeZ,
                Roll = t.Roll, Pitch = t.Pitch, Yaw = t.Yaw,
                GpsConnected = t.GpsConnected, ObstacleDetected = t.ObstacleDetected,
                WindLevel = t.WindLevel, CameraOk = t.CameraOk, MotorsOk = t.MotorsOk,
                MotorRpms = t.MotorRpms, FlightMode = t.FlightMode, HmsMessages = t.HmsMessage
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogWarning("Telemetri kaydedilemedi: {msg}", ex.Message); }
    }
}
