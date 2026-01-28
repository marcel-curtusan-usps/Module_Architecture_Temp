using System.Diagnostics;

namespace MainApp.Models;

public class ModuleProcess
{
    public required string Name { get; set; }
    public required int Port { get; set; }
    public required Process Process { get; set; }
    public required DateTime StartTime { get; set; }
    
    // Heartbeat tracking properties
    public DateTime? LastHeartbeatTime { get; set; }
    public bool IsHealthy { get; set; } = true;

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
