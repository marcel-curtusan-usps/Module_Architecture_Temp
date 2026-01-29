using System.Diagnostics;

namespace MainApp.Models;

public class ModuleProcess
{
    private readonly object _lockObject = new();
    private DateTime? _lastHeartbeatTime;
    private bool _isHealthy = true;

    public required string Name { get; set; }
    public required int Port { get; set; }
    public required Process Process { get; set; }
    public required DateTime StartTime { get; set; }
    
    // Thread-safe heartbeat tracking properties
    public DateTime? LastHeartbeatTime
    {
        get
        {
            lock (_lockObject)
            {
                return _lastHeartbeatTime;
            }
        }
        set
        {
            lock (_lockObject)
            {
                _lastHeartbeatTime = value;
            }
        }
    }

    public bool IsHealthy
    {
        get
        {
            lock (_lockObject)
            {
                return _isHealthy;
            }
        }
        set
        {
            lock (_lockObject)
            {
                _isHealthy = value;
            }
        }
    }

    public string Status => Process.HasExited ? "Stopped" : "Running";
    
    public string Uptime
    {
        get
        {
            var uptime = DateTime.Now - StartTime;
            return $"{uptime:hh\\:mm\\:ss}";
        }
    }
}
