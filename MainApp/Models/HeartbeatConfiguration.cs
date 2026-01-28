using System.ComponentModel.DataAnnotations;

namespace MainApp.Models;

/// <summary>
/// Configuration for heartbeat monitoring
/// </summary>
public class HeartbeatConfiguration
{
    /// <summary>
    /// The interval in milliseconds at which modules should send heartbeats (default: 1000ms = 1 second)
    /// </summary>
    [Range(100, 60000, ErrorMessage = "HeartbeatIntervalMs must be between 100 and 60000 milliseconds")]
    public int HeartbeatIntervalMs { get; set; } = 1000;

    /// <summary>
    /// The timeout in milliseconds after which a module is considered unhealthy if no heartbeat is received (default: 5000ms = 5 seconds)
    /// </summary>
    [Range(1000, 300000, ErrorMessage = "HeartbeatTimeoutMs must be between 1000 and 300000 milliseconds")]
    public int HeartbeatTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// The interval in milliseconds at which the main app checks for unhealthy modules (default: 1000ms = 1 second)
    /// </summary>
    [Range(100, 60000, ErrorMessage = "MonitoringIntervalMs must be between 100 and 60000 milliseconds")]
    public int MonitoringIntervalMs { get; set; } = 1000;
}
