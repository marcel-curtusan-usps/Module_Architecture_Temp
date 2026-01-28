using System.Diagnostics;

namespace ContainerDetectionModule.Services;

/// <summary>
/// Monitors the parent process and gracefully terminates the current process
/// if the parent is no longer running. This prevents orphaned processes.
/// </summary>
public class ParentProcessMonitor : IDisposable
{
    private readonly int _parentProcessId;
    private readonly ILogger<ParentProcessMonitor> _logger;
    private readonly Timer? _checkTimer;
    private readonly int _checkIntervalSeconds;
    private readonly string? _expectedParentProcessName;
    private bool _disposed;
    private readonly IHostApplicationLifetime _lifetime;

    /// <summary>
    /// Creates a new parent process monitor.
    /// </summary>
    /// <param name="parentProcessId">The process ID of the parent process to monitor</param>
    /// <param name="logger">Logger for diagnostic messages</param>
    /// <param name="lifetime">Application lifetime to trigger graceful shutdown</param>
    /// <param name="checkIntervalSeconds">How often to check parent process status (default: 5 seconds)</param>
    /// <param name="expectedParentProcessName">Optional: Expected parent process name for additional validation</param>
    public ParentProcessMonitor(
        int parentProcessId,
        ILogger<ParentProcessMonitor> logger,
        IHostApplicationLifetime lifetime,
        int checkIntervalSeconds = 5,
        string? expectedParentProcessName = null)
    {
        _parentProcessId = parentProcessId;
        _logger = logger;
        _lifetime = lifetime;
        _checkIntervalSeconds = checkIntervalSeconds;
        _expectedParentProcessName = expectedParentProcessName;

        _logger.LogInformation(
            "ParentProcessMonitor initialized. Monitoring parent process ID: {ParentPID}, Check interval: {Interval}s",
            _parentProcessId,
            _checkIntervalSeconds);

        // Validate parent process exists at startup
        if (!IsParentProcessRunning())
        {
            _logger.LogWarning(
                "Parent process with PID {ParentPID} is not running at monitor initialization. This module may be orphaned.",
                _parentProcessId);
        }

        // Start the periodic check timer
        _checkTimer = new Timer(
            CheckParentProcess,
            null,
            TimeSpan.FromSeconds(_checkIntervalSeconds),
            TimeSpan.FromSeconds(_checkIntervalSeconds));
    }

    /// <summary>
    /// Checks if the parent process is still running.
    /// Handles cross-platform differences and validates process name if provided.
    /// </summary>
    private bool IsParentProcessRunning()
    {
        try
        {
            // Try to get the process by ID
            var process = Process.GetProcessById(_parentProcessId);
            
            // Process exists, but verify it hasn't exited
            if (process.HasExited)
            {
                return false;
            }

            // Additional validation: check process name if provided
            // This helps prevent false positives from PID reuse
            if (!string.IsNullOrEmpty(_expectedParentProcessName))
            {
                try
                {
                    var processName = process.ProcessName;
                    
                    // Compare process names (case-insensitive, handle potential .exe extension on Windows)
                    var expectedName = _expectedParentProcessName.Replace(".exe", "", StringComparison.OrdinalIgnoreCase);
                    var actualName = processName.Replace(".exe", "", StringComparison.OrdinalIgnoreCase);
                    
                    if (!actualName.Equals(expectedName, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning(
                            "Process with PID {ParentPID} exists but has name '{ActualName}', expected '{ExpectedName}'. " +
                            "This may indicate PID reuse.",
                            _parentProcessId,
                            actualName,
                            expectedName);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to verify parent process name for PID {ParentPID}. Assuming process is valid.",
                        _parentProcessId);
                }
            }

            return true;
        }
        catch (ArgumentException)
        {
            // Process with given ID does not exist
            return false;
        }
        catch (InvalidOperationException)
        {
            // Process has exited or access is denied
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error while checking parent process {ParentPID}",
                _parentProcessId);
            return false;
        }
    }

    /// <summary>
    /// Timer callback that checks parent process status and initiates shutdown if needed.
    /// </summary>
    private void CheckParentProcess(object? state)
    {
        if (_disposed)
        {
            return;
        }

        if (!IsParentProcessRunning())
        {
            _logger.LogWarning(
                "Parent process (PID: {ParentPID}) is no longer running. Initiating graceful shutdown to prevent orphaned process.",
                _parentProcessId);

            // Stop the timer to prevent additional checks
            _checkTimer?.Change(Timeout.Infinite, Timeout.Infinite);

            // Trigger graceful application shutdown
            _lifetime.StopApplication();
        }
    }

    /// <summary>
    /// Starts monitoring the parent process.
    /// This method is provided for consistency but monitoring starts automatically in the constructor.
    /// </summary>
    public void Start()
    {
        _logger.LogInformation("Parent process monitoring is active.");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _checkTimer?.Dispose();
        
        _logger.LogInformation("ParentProcessMonitor disposed.");
    }
}
