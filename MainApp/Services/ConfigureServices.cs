using MainApp.Models;

namespace MainApp.Services;

public static class ConfigureServices
{
    /// <summary>
    /// Starts modules from configuration in the configured order.
    /// </summary>
    /// <param name="moduleManager">The module manager service to use for starting modules</param>
    /// <param name="configuration">The application configuration containing ModuleConfiguration section</param>
    /// <param name="logger">Logger for startup operations</param>
    public static async Task StartModulesFromConfiguration(
        ModuleManagerService moduleManager,
        IConfiguration configuration,
        ILogger logger)
    {
        var moduleConfig = configuration.GetSection("ModuleConfiguration").Get<ModuleConfiguration>();

        if (moduleConfig?.StartupModules == null || moduleConfig.StartupModules.Count == 0)
        {
            logger.LogInformation("No modules configured for startup.");
            return;
        }

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
}
