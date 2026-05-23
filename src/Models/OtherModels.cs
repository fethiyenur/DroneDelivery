using System.ComponentModel.DataAnnotations;

namespace DroneKurye.Models;

public class DroneDataHistory
{
    public long Id { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    // Konum
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Altitude { get; set; }

    // Uçuş
    public double Speed { get; set; }
    public int BatteryPercent { get; set; }
    public int SignalStrength { get; set; }

    // RTK
    public string RtkStatus { get; set; } = "None"; // Fix / Float / None
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
    public string? HmsMessages { get; set; } // JSON
    public string FlightMode { get; set; } = "Normal"; // Normal / Failsafe / ReturnToHome
}

public class DeliveryOrder
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string RecipientName { get; set; } = string.Empty;

    public double DestinationLat { get; set; }
    public double DestinationLng { get; set; }

    public DeliveryStatus Status { get; set; } = DeliveryStatus.Preparing;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // Araç/Drone konumları (son bilinen)
    public double? VehicleLat { get; set; }
    public double? VehicleLng { get; set; }
    public double? DroneLat { get; set; }
    public double? DroneLng { get; set; }
}

public enum DeliveryStatus
{
    Preparing = 0,
    OnTheWay = 1,
    DroneDelivering = 2,
    Completed = 3
}

public class Advertisement
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string ImageUrl { get; set; } = string.Empty;

    public string? LinkUrl { get; set; }

    public AdPlacement Placement { get; set; } = AdPlacement.Banner;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
}

public enum AdPlacement
{
    Banner = 0,
    Sidebar = 1
}

public class Notification
{
    public int Id { get; set; }
    public int? UserId { get; set; } // null = broadcast tüm kullanıcılara

    public NotificationType Type { get; set; }

    [Required, MaxLength(300)]
    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
}

public enum NotificationType
{
    LowBattery = 0,
    ObstacleDetected = 1,
    ConnectionLost = 2,
    Failsafe = 3,
    ReturnToHome = 4,
    General = 5
}
