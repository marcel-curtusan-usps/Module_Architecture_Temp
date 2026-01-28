using MainApp.Models;
using MainApp.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register the module manager as a singleton
builder.Services.AddSingleton<ModuleManagerService>();

// Register heartbeat monitoring service
builder.Services.AddHostedService<HeartbeatMonitorService>();

// Bind configurations from appsettings
builder.Services.Configure<HeartbeatConfiguration>(
    builder.Configuration.GetSection("HeartbeatConfiguration"));
builder.Services.Configure<ModuleConfiguration>(
    builder.Configuration.GetSection("ModuleConfiguration"));

var app = builder.Build();

// Serve static files from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

// Derive a process name from the entry assembly or application name
var processName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name
    ?? builder.Environment.ApplicationName ?? "MainApp";
try
{
    Console.Title = processName;
}
catch { }

// Configure the HTTP request pipeline - Enable OpenAPI and Scalar UI
app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Start modules from configuration at startup
var moduleManager = app.Services.GetRequiredService<ModuleManagerService>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Process name: {ProcessName}", processName);

await ConfigureServices.StartModulesFromConfiguration(
    moduleManager,
    builder.Configuration,
    logger);

// Graceful shutdown - stop all modules when the application stops
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    moduleManager.StopAllModulesAsync().GetAwaiter().GetResult();
});

await app.RunAsync();
