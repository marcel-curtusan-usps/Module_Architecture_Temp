using MainApp.Models;
using MainApp.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register the module manager as a singleton
builder.Services.AddSingleton<ModuleManagerService>();

// Bind ModuleConfiguration from appsettings
builder.Services.Configure<ModuleConfiguration>(
    builder.Configuration.GetSection("ModuleConfiguration"));

var app = builder.Build();

// Serve static files from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline - Enable OpenAPI and Scalar UI
app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Start modules from configuration at startup
var moduleManager = app.Services.GetRequiredService<ModuleManagerService>();
var moduleConfig = builder.Configuration.GetSection("ModuleConfiguration").Get<ModuleConfiguration>();

if (moduleConfig?.StartupModules != null && moduleConfig.StartupModules.Count > 0)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Starting modules from configuration...");
    
    foreach (var moduleSettings in moduleConfig.StartupModules)
    {
        try
        {
            await moduleManager.StartModuleAsync(
                moduleSettings.Name,
                moduleSettings.Port,
                moduleSettings.ProjectPath);
                
            logger.LogInformation(
                "Module '{ModuleName}' configured to start on port {Port}",
                moduleSettings.Name,
                moduleSettings.Port);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, 
                "Failed to start module '{ModuleName}' from configuration. Module may not be built or available.",
                moduleSettings.Name);
        }
    }
    
    if (!string.IsNullOrEmpty(moduleConfig.DefaultModule))
    {
        logger.LogInformation("Default module: {DefaultModule}", moduleConfig.DefaultModule);
    }
}

// Graceful shutdown - stop all modules when the application stops
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    moduleManager.StopAllModulesAsync().GetAwaiter().GetResult();
});

app.Run();
