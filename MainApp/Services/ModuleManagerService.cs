using System.Diagnostics;
using MainApp.Models;

namespace MainApp.Services;

public class ModuleManagerService
{
    private readonly List<ModuleProcess> _modules = new();
    private readonly HttpClient _httpClient = new();
    private int _nextPort = 5000;
    private readonly ILogger<ModuleManagerService> _logger;
    private const int PortReleaseDelayMs = 500;

    public ModuleManagerService(ILogger<ModuleManagerService> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<ModuleProcess> GetAllModules()
    {
        return _modules.AsReadOnly();
    }

    public ModuleProcess? GetModuleByName(string name)
    {
        return _modules.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Terminates an existing module process if it's running.
    /// </summary>
    private async Task TerminateExistingModuleAsync(ModuleProcess existingModule)
    {
        _logger.LogInformation("Module '{Name}' is already running (PID: {ProcessId}). Killing existing process...", 
            existingModule.Name, existingModule.Process.Id);
        
        if (!existingModule.Process.HasExited)
        {
            try
            {
                existingModule.Process.Kill();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to kill existing module '{Name}' process", existingModule.Name);
            }
        }
        
        _modules.Remove(existingModule);
        _logger.LogInformation("Existing module '{Name}' removed from tracking.", existingModule.Name);
        
        // Small delay to ensure port is released
        await Task.Delay(PortReleaseDelayMs);
    }

    public async Task<ModuleProcess> StartModuleAsync(string name)
    {
        // Check if a module with this name is already running
        var existingModule = GetModuleByName(name);
        if (existingModule != null)
        {
            await TerminateExistingModuleAsync(existingModule);
        }

        var port = _nextPort++;
        var modulePath = FindModulePath(name);
        
        if (modulePath == null)
        {
            throw new FileNotFoundException($"Could not find module '{name}'. Please build the solution first.");
        }

        return await StartModuleAsync(name, port, modulePath);
    }

    public async Task<ModuleProcess> StartModuleAsync(string name, int port, string? projectPath = null)
    {
        // Check if a module with this name is already running
        var existingModule = GetModuleByName(name);
        if (existingModule != null)
        {
            await TerminateExistingModuleAsync(existingModule);
        }

        var modulePath = projectPath ?? FindModulePath(name);
        
        if (modulePath == null)
        {
            throw new FileNotFoundException($"Could not find module '{name}'. Please build the solution first.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{modulePath} --name {name} --port {port}",
            UseShellExecute = false,
            CreateNoWindow = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start module process.");
        }

        var module = new ModuleProcess
        {
            Name = name,
            Port = port,
            Process = process,
            StartTime = DateTime.Now
        };
        
        _modules.Add(module);
        
        // Give it time to start
        await Task.Delay(1000);
        
        _logger.LogInformation("Module '{Name}' started on port {Port} (PID: {ProcessId})", name, port, process.Id);
        
        return module;
    }

    public async Task<bool> StopModuleAsync(string name)
    {
        var module = GetModuleByName(name);
        if (module == null)
        {
            return false;
        }

        try
        {
            var response = await _httpClient.PostAsync($"http://localhost:{module.Port}/stop", null);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Stop request sent to module '{Name}'. Waiting for graceful shutdown...", name);
                
                if (!module.Process.WaitForExit(5000))
                {
                    _logger.LogWarning("Module '{Name}' did not stop in time. Force killing...", name);
                    if (!module.Process.HasExited)
                    {
                        try
                        {
                            module.Process.Kill();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to kill module '{Name}'", name);
                        }
                    }
                }
                
                _modules.Remove(module);
                _logger.LogInformation("Module '{Name}' stopped.", name);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping module '{Name}'. Attempting force kill...", name);
            if (!module.Process.HasExited)
            {
                try
                {
                    module.Process.Kill();
                }
                catch (Exception killEx)
                {
                    _logger.LogError(killEx, "Failed to kill module '{Name}'", name);
                }
                _modules.Remove(module);
                return true;
            }
        }

        return false;
    }

    public async Task<bool> RestartModuleAsync(string name)
    {
        var module = GetModuleByName(name);
        if (module == null)
        {
            return false;
        }

        var port = module.Port;

        _logger.LogInformation("Restarting module '{Name}'...", name);
        
        // Stop the module
        try
        {
            await _httpClient.PostAsync($"http://localhost:{port}/stop", null);
            module.Process.WaitForExit(3000);
        }
        catch { }

        if (!module.Process.HasExited)
        {
            try
            {
                module.Process.Kill();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to kill module '{Name}' during restart", name);
            }
        }
        _modules.Remove(module);

        // Start it again
        await Task.Delay(500);
        
        var modulePath = FindModulePath(name);
        if (modulePath == null)
        {
            throw new FileNotFoundException($"Could not find module '{name}'.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{modulePath} --name {name} --port {port}",
            UseShellExecute = false,
            CreateNoWindow = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        var process = Process.Start(startInfo);
        if (process != null)
        {
            var newModule = new ModuleProcess
            {
                Name = name,
                Port = port,
                Process = process,
                StartTime = DateTime.Now
            };
            _modules.Add(newModule);
            
            await Task.Delay(1000);
            _logger.LogInformation("Module '{Name}' restarted on port {Port}", name, port);
            return true;
        }

        return false;
    }

    public bool KillModule(string name)
    {
        var module = GetModuleByName(name);
        if (module == null)
        {
            return false;
        }

        if (!module.Process.HasExited)
        {
            try
            {
                module.Process.Kill();
                _logger.LogInformation("Module '{Name}' force killed.", name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to kill module '{Name}'", name);
                return false;
            }
        }
        else
        {
            _logger.LogInformation("Module '{Name}' is already stopped.", name);
        }
        
        _modules.Remove(module);
        return true;
    }

    public async Task StopAllModulesAsync()
    {
        _logger.LogInformation("Stopping all modules...");
        foreach (var module in _modules.ToList())
        {
            try
            {
                await _httpClient.PostAsync($"http://localhost:{module.Port}/stop", null);
                module.Process.WaitForExit(2000);
            }
            catch { }

            if (!module.Process.HasExited)
            {
                module.Process.Kill();
            }
        }
        _modules.Clear();
        _logger.LogInformation("All modules stopped.");
    }

    private static string? FindModulePath(string moduleName)
    {
        // Look for {moduleName}.dll in common build locations
        var basePath = Directory.GetCurrentDirectory();
        var targetFramework = "net10.0";
        var possiblePaths = new[]
        {
            Path.Combine(basePath, "..", moduleName, "bin", "Debug", targetFramework, $"{moduleName}.dll"),
            Path.Combine(basePath, "..", moduleName, "bin", "Release", targetFramework, $"{moduleName}.dll"),
            Path.Combine(basePath, moduleName, "bin", "Debug", targetFramework, $"{moduleName}.dll"),
            Path.Combine(basePath, moduleName, "bin", "Release", targetFramework, $"{moduleName}.dll")
        };

        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
                return fullPath;
        }

        return null;
    }
}
