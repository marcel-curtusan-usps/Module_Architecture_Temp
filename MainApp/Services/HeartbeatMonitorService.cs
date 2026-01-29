using MainApp.Models;
using Microsoft.Extensions.Options;

namespace MainApp.Services;

/// <summary>
/// Background service that monitors module heartbeats and logs unhealthy modules
/// </summary>
public class HeartbeatMonitorService : BackgroundService
{
    private readonly ModuleManagerService _moduleManager;
    private readonly ILogger<HeartbeatMonitorService> _logger;
    private readonly HeartbeatConfiguration _config;

    public HeartbeatMonitorService(
        ModuleManagerService moduleManager,
        ILogger<HeartbeatMonitorService> logger,
        IOptions<HeartbeatConfiguration> config)
    {
        _moduleManager = moduleManager;
        _logger = logger;
        _config = config.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Heartbeat monitor service started. Timeout: {Timeout}ms, Check interval: {Interval}ms",
            _config.HeartbeatTimeoutMs, _config.MonitoringIntervalMs);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorModuleHeartbeatsAsync();
                await Task.Delay(_config.MonitoringIntervalMs, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring module heartbeats");
                await Task.Delay(1000, stoppingToken); // Brief pause before retrying
            }
        }

        _logger.LogInformation("Heartbeat monitor service stopped.");
    }

    private Task MonitorModuleHeartbeatsAsync()
    {
        var modules = _moduleManager.GetAllModules();
        var now = DateTime.UtcNow;

        foreach (var module in modules)
        {
            // Skip modules that have already exited (with exception handling)
            try
            {
                if (module.Process.HasExited)
                {
                    continue;
                }
            }
            catch (InvalidOperationException)
            {
                // Process handle is no longer valid
                _logger.LogWarning("Module '{ModuleName}' process handle is no longer valid", module.Name);
                continue;
            }

            // If module has never sent a heartbeat, give it some grace time
            if (module.LastHeartbeatTime == null)
            {
                // Allow modules time to start up and send first heartbeat
                var timeSinceStart = now - module.StartTime;
                if (timeSinceStart.TotalMilliseconds > _config.HeartbeatTimeoutMs * 2)
                {
                    _logger.LogWarning("Module '{ModuleName}' has not sent any heartbeat since startup ({TimeSinceStart}ms ago)",
                        module.Name, timeSinceStart.TotalMilliseconds);
                }
                continue;
            }

            // Check if heartbeat is stale
            var timeSinceLastHeartbeat = now - module.LastHeartbeatTime.Value;
            if (timeSinceLastHeartbeat.TotalMilliseconds > _config.HeartbeatTimeoutMs)
            {
                if (module.IsHealthy)
                {
                    // Mark as unhealthy and log the event
                    module.IsHealthy = false;
                    _logger.LogWarning("Module '{ModuleName}' is now UNHEALTHY. Last heartbeat was {TimeSinceLastHeartbeat}ms ago (timeout: {Timeout}ms)",
                        module.Name, timeSinceLastHeartbeat.TotalMilliseconds, _config.HeartbeatTimeoutMs);
                }
            }
            else
            {
                // Heartbeat is recent, module is healthy
                if (!module.IsHealthy)
                {
                    // Module recovered
                    module.IsHealthy = true;
                    _logger.LogInformation("Module '{ModuleName}' is now HEALTHY again",
                        module.Name);
                }
            }
        }

        return Task.CompletedTask;
    }
}
