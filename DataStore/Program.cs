using DataStore.Services;
using Scalar.AspNetCore;
using SharedServices;

var builder = WebApplication.CreateBuilder(args);

// Parse command line arguments to get the port and name
var port = 5000; // Default port
var moduleName = "DataStore"; // Default name

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
    port);
heartbeatService.StartHeartbeat();

// Ensure heartbeat service is disposed on shutdown
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    heartbeatService.StopHeartbeat();
    heartbeatService.Dispose();
});

app.Run();
