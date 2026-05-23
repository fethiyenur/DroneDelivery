namespace DroneKurye.Models;

public class TelemetryData
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Konum
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Altitude { get; set; }

    // Uçuş
    public double Speed { get; set; }
    public int BatteryPercent { get; set; }
    public int SignalStrength { get; set; }

    // RTK
    public string RtkStatus { get; set; } = "None";
    public bool BaseStationConnected { get; set; }
    public double BaseStationDistance { get; set; }

    // IMU
    public double AccelerationX { get; set; }
    public double AccelerationY { get; set; }
    public double AccelerationZ { get; set; }
    public double GyroscopeX { get; set; }
    public double GyroscopeY { get; set; }
    public double GyroscopeZ { get; set; }

    // Yönelim
    public double Roll { get; set; }
    public double Pitch { get; set; }
    public double Yaw { get; set; }

    // Sistem sağlığı
    public bool GpsConnected { get; set; }
    public bool ObstacleDetected { get; set; }
    public int WindLevel { get; set; }
    public bool CameraOk { get; set; }
    public bool MotorsOk { get; set; }
    public int[] MotorRpms { get; set; } = new int[4];
    public string FlightMode { get; set; } = "Normal";
    public string? HmsMessage { get; set; }

    // Araç konumu
    public double VehicleLat { get; set; }
    public double VehicleLng { get; set; }
}
