using System.Net.Http.Json;

namespace SharedServices;

/// <summary>
/// Base service for sending periodic heartbeat signals to the main application
/// </summary>
public class HeartbeatService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly string _moduleName;
    private readonly int _modulePort;
    private readonly int _heartbeatIntervalMs;
    private readonly string _mainAppUrl;
    private Timer? _heartbeatTimer;
    private bool _disposed;
    private int _failedHeartbeatCount;
    private readonly int _maxFailedHeartbeats = 3; // Terminate after 3 failed heartbeats
    private int _isHeartbeatInProgress; // 0 = not in progress, 1 = in progress

    /// <summary>
    /// Initializes a new instance of the HeartbeatService
    /// </summary>
    /// <param name="httpClient">HttpClient for sending requests</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="moduleName">Name of the module</param>
    /// <param name="modulePort">Port on which the module is running</param>
    /// <param name="mainAppUrl">URL of the main application (default: http://localhost:5000)</param>
    /// <param name="heartbeatIntervalMs">Interval in milliseconds between heartbeats (default: 1000ms)</param>
    public HeartbeatService(
        HttpClient httpClient,
        ILogger logger,
        string moduleName,
        int modulePort,
        string mainAppUrl = "http://localhost:5000",
        int heartbeatIntervalMs = 1000)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _moduleName = moduleName ?? throw new ArgumentNullException(nameof(moduleName));
        _modulePort = modulePort;
        _mainAppUrl = mainAppUrl;
        _heartbeatIntervalMs = heartbeatIntervalMs;
        _failedHeartbeatCount = 0;
        _isHeartbeatInProgress = 0;
        
        // Configure HttpClient timeout to ensure timely failure detection
        _httpClient.Timeout = TimeSpan.FromSeconds(3);
    }

    /// <summary>
    /// Starts sending periodic heartbeat signals to the main application
    /// </summary>
    public void StartHeartbeat()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(HeartbeatService));
        }

        _logger.LogInformation("Starting heartbeat service for module '{ModuleName}' on port {Port}. Interval: {Interval}ms",
            _moduleName, _modulePort, _heartbeatIntervalMs);

        // Start the timer to send heartbeats periodically
        _heartbeatTimer = new Timer(
            SendHeartbeatCallback,
            null,
            TimeSpan.Zero, // Send first heartbeat immediately
            TimeSpan.FromMilliseconds(_heartbeatIntervalMs));
    }

    /// <summary>
    /// Stops sending heartbeat signals
    /// </summary>
    public void StopHeartbeat()
    {
        if (_heartbeatTimer != null)
        {
            _heartbeatTimer.Dispose();
            _heartbeatTimer = null;
            _logger.LogInformation("Heartbeat service stopped for module '{ModuleName}'", _moduleName);
        }
    }

    private async void SendHeartbeatCallback(object? state)
    {
        // Prevent overlapping heartbeat attempts using atomic compare-exchange
        if (Interlocked.CompareExchange(ref _isHeartbeatInProgress, 1, 0) != 0)
        {
            // Another heartbeat is already in progress, skip this one
            _logger.LogDebug("Skipping heartbeat for module '{ModuleName}' - previous heartbeat still in progress", _moduleName);
            return;
        }

        try
        {
            await SendHeartbeatAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in heartbeat callback for module '{ModuleName}'", _moduleName);
        }
        finally
        {
            // Release the lock
            Interlocked.Exchange(ref _isHeartbeatInProgress, 0);
        }
    }

    private async Task SendHeartbeatAsync()
    {
        try
        {
            var heartbeatData = new
            {
                ModuleName = _moduleName,
                Port = _modulePort,
                Message = "Heartbeat",
                Timestamp = DateTime.UtcNow
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_mainAppUrl}/api/module-messages/heartbeat",
                heartbeatData);

            if (response.IsSuccessStatusCode)
            {
                // Heartbeat acknowledged successfully - reset counter using thread-safe operation
                Interlocked.Exchange(ref _failedHeartbeatCount, 0);
                _logger.LogDebug("Heartbeat sent successfully from module '{ModuleName}'", _moduleName);
            }
            else
            {
                // Heartbeat failed with non-success status code - increment counter thread-safely
                var currentCount = Interlocked.Increment(ref _failedHeartbeatCount);
                _logger.LogWarning("Heartbeat failed for module '{ModuleName}'. Status: {StatusCode}. Failed count: {FailedCount}",
                    _moduleName, response.StatusCode, currentCount);

                await HandleFailedHeartbeatAsync();
            }
        }
        catch (HttpRequestException ex)
        {
            // Network-related error - increment counter thread-safely
            var currentCount = Interlocked.Increment(ref _failedHeartbeatCount);
            _logger.LogWarning(ex, "Heartbeat communication error for module '{ModuleName}'. Failed count: {FailedCount}",
                _moduleName, currentCount);

            await HandleFailedHeartbeatAsync();
        }
        catch (TaskCanceledException ex)
        {
            // Timeout - increment counter thread-safely
            var currentCount = Interlocked.Increment(ref _failedHeartbeatCount);
            _logger.LogWarning(ex, "Heartbeat timeout for module '{ModuleName}'. Failed count: {FailedCount}",
                _moduleName, currentCount);

            await HandleFailedHeartbeatAsync();
        }
        catch (Exception ex)
        {
            // Unexpected error - increment counter thread-safely
            var currentCount = Interlocked.Increment(ref _failedHeartbeatCount);
            _logger.LogError(ex, "Unexpected error sending heartbeat from module '{ModuleName}'. Failed count: {FailedCount}",
                _moduleName, currentCount);

            await HandleFailedHeartbeatAsync();
        }
    }

    private async Task HandleFailedHeartbeatAsync()
    {
        var currentCount = Interlocked.CompareExchange(ref _failedHeartbeatCount, 0, 0); // Read the current value
        if (currentCount >= _maxFailedHeartbeats)
        {
            _logger.LogCritical("Module '{ModuleName}' has failed to send heartbeat {FailedCount} times. Main application may be unreachable. Terminating module...",
                _moduleName, currentCount);

            // Stop the heartbeat timer to prevent further attempts
            StopHeartbeat();

            // Give the log message time to be written
            await Task.Delay(100);

            // Terminate the module
            Environment.Exit(1);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            StopHeartbeat();
            _disposed = true;
        }
    }
}
