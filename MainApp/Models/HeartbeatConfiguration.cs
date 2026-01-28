namespace MainApp.Models;

/// <summary>
/// Configuration for heartbeat monitoring
/// </summary>
public class HeartbeatConfiguration
{
    /// <summary>
    /// The interval in milliseconds at which modules should send heartbeats (default: 1000ms = 1 second)
    /// </summary>
    public int HeartbeatIntervalMs { get; set; } = 1000;

    /// <summary>
    /// The timeout in milliseconds after which a module is considered unhealthy if no heartbeat is received (default: 5000ms = 5 seconds)
    /// </summary>
    public int HeartbeatTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// The interval in milliseconds at which the main app checks for unhealthy modules (default: 1000ms = 1 second)
    /// </summary>
    public int MonitoringIntervalMs { get; set; } = 1000;
}
