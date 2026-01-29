using DataStore.Services;
using Scalar.AspNetCore;
using SharedServices;

var builder = WebApplication.CreateBuilder(args);

// Parse command line arguments to get the port and name
var port = 5000; // Default port
var moduleName = "DataStore"; // Default name
var mainAppUrl = "http://localhost:5000"; // Default MainApp URL
int? parentPid = null;

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--port" && i + 1 < args.Length)
    {
        if (int.TryParse(args[i + 1], out var parsedPort))
        {
            port = parsedPort;
        }
    }
    else if (args[i] == "--name" && i + 1 < args.Length)
    {
        moduleName = args[i + 1];
    }
    else if (args[i] == "--mainappurl" && i + 1 < args.Length)
    {
        mainAppUrl = args[i + 1];
    }
}

// Configure the web host to use the specified port
builder.WebHost.UseUrls($"http://localhost:{port}");

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register HttpClient for heartbeat service
builder.Services.AddHttpClient();

// Register the data provider selector as a singleton
builder.Services.AddSingleton<DataProviderSelector>();

var app = builder.Build();

// Initialize parent process monitor if parent PID was provided
if (parentPid.HasValue)
{
    var configuration = app.Services.GetRequiredService<IConfiguration>();
    
    // Read check interval from configuration, default to 5 seconds
    var checkIntervalSeconds = configuration.GetValue<int>("ParentProcessMonitor:CheckIntervalSeconds", 5);
    
    // Register the monitor as a singleton so it can be properly disposed
    var monitor = ActivatorUtilities.CreateInstance<ParentProcessMonitor>(
        app.Services,
        parentPid.Value,
        checkIntervalSeconds,
        "MainApp");
    
    // Register it in the service collection for disposal
    app.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.Register(() =>
    {
        monitor.Dispose();
    });
    
    monitor.Start();
    
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation(
        "DataStore started with parent process monitoring (Parent PID: {ParentPID}, Check Interval: {Interval}s)",
        parentPid.Value,
        checkIntervalSeconds);
}
else
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning(
        "DataStore started without parent process monitoring. " +
        "Module will not auto-terminate if parent process stops.");
}

// Configure the HTTP request pipeline - Enable OpenAPI and Scalar UI
app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Initialize the data provider on startup
var providerSelector = app.Services.GetRequiredService<DataProviderSelector>();
await providerSelector.GetProviderAsync();

// Start heartbeat service
var httpClientFactory = app.Services.GetRequiredService<IHttpClientFactory>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var heartbeatService = new HeartbeatService(
    httpClientFactory.CreateClient(),
    logger,
    moduleName,
    port,
    mainAppUrl);
heartbeatService.StartHeartbeat();

// Ensure heartbeat service is disposed on shutdown
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    heartbeatService.StopHeartbeat();
    heartbeatService.Dispose();
});

app.Run();
