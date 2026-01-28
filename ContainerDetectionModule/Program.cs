using ContainerDetectionModule.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Parse command line arguments to get the port and parent PID
var port = 5000; // Default port
var nameArg = "";
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
        nameArg = args[i + 1];
    }
    else if (args[i] == "--parent-pid" && i + 1 < args.Length)
    {
        if (int.TryParse(args[i + 1], out var parsedPid))
        {
            parentPid = parsedPid;
        }
    }
}

// Configure the web host to use the specified port
builder.WebHost.UseUrls($"http://localhost:{port}");

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register detection services as singletons
builder.Services.AddSingleton<DetectionService>();
builder.Services.AddSingleton<BuiltInVisionService>();
builder.Services.AddSingleton<FoundryVisionService>();

var app = builder.Build();

// Initialize parent process monitor if parent PID was provided
if (parentPid.HasValue)
{
    var logger = app.Services.GetRequiredService<ILogger<ParentProcessMonitor>>();
    var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
    var configuration = app.Services.GetRequiredService<IConfiguration>();
    
    // Read check interval from configuration, default to 5 seconds
    var checkIntervalSeconds = configuration.GetValue<int>("ParentProcessMonitor:CheckIntervalSeconds", 5);
    
    // Create and start the parent process monitor
    // Expected parent process name: "MainApp" (for PID reuse detection)
    var monitor = new ParentProcessMonitor(
        parentPid.Value,
        logger,
        lifetime,
        checkIntervalSeconds: checkIntervalSeconds,
        expectedParentProcessName: "MainApp");
    
    monitor.Start();
    
    logger.LogInformation(
        "ContainerDetectionModule started with parent process monitoring (Parent PID: {ParentPID}, Check Interval: {Interval}s)",
        parentPid.Value,
        checkIntervalSeconds);
}
else
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning(
        "ContainerDetectionModule started without parent process monitoring. " +
        "Module will not auto-terminate if parent process stops.");
}

// Configure the HTTP request pipeline - Enable OpenAPI and Scalar UI
app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
