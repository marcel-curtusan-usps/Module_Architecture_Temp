using System.Collections.Concurrent;
using System.Diagnostics;
using MainApp.Models;

namespace MainApp.Services;

public class ModuleManagerService
{
    private readonly ConcurrentDictionary<string, ModuleProcess> _modules = new(StringComparer.OrdinalIgnoreCase);
    private readonly HttpClient _httpClient = new();
    private int _nextPort = 5000;
    private readonly ILogger<ModuleManagerService> _logger;
    private const int PortReleaseDelayMs = 500;
    private readonly string _mainAppUrl;

    public ModuleManagerService(ILogger<ModuleManagerService> logger, IConfiguration configuration)
    {
        _logger = logger;
        
        // Get the MainApp URL from configuration or use default
        var urls = configuration["ASPNETCORE_URLS"] ?? configuration["urls"] ?? "http://localhost:5000";
        _mainAppUrl = urls.Split(';')[0]; // Use the first URL if multiple are specified
        
        _logger.LogInformation("ModuleManagerService initialized. MainApp URL: {MainAppUrl}", _mainAppUrl);
    }

    public IReadOnlyList<ModuleProcess> GetAllModules()
    {
        return _modules.Values.ToList().AsReadOnly();
    }

    public ModuleProcess? GetModuleByName(string name)
    {
        _modules.TryGetValue(name, out var module);
        return module;
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
        
        _modules.TryRemove(existingModule.Name, out _);
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

        // Get the current process ID to pass to the child module
        var parentProcessId = Environment.ProcessId;

        var startInfo = new ProcessStartInfo
        {
            UseShellExecute = false,
            CreateNoWindow = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        // If modulePath points to a built DLL, run it with `dotnet <dll>`.
        // If it points to a project folder or .csproj, use `dotnet run --project`.
        if (modulePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            startInfo.FileName = "dotnet";
            startInfo.Arguments = $"\"{modulePath}\" --name {name} --port {port} --mainappurl {_mainAppUrl}";
            startInfo.WorkingDirectory = Path.GetDirectoryName(modulePath) ?? Directory.GetCurrentDirectory();
        }
        else if (Directory.Exists(modulePath) || modulePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            startInfo.FileName = "dotnet";
            if (modulePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                startInfo.Arguments = $"run --project \"{modulePath}\" --no-build -- --name {name} --port {port} --mainappurl {_mainAppUrl}";
                startInfo.WorkingDirectory = Path.GetDirectoryName(modulePath) ?? Directory.GetCurrentDirectory();
            }
            else
            {
                // modulePath is a directory
                startInfo.Arguments = $"run --project \"{modulePath}\" -- --name {name} --port {port} --mainappurl {_mainAppUrl}";
                startInfo.WorkingDirectory = modulePath;
            }
        }
        else
        {
            // Fallback: treat modulePath as an executable path
            startInfo.FileName = modulePath;
            startInfo.Arguments = $"--name {name} --port {port} --mainappurl {_mainAppUrl}";
            startInfo.WorkingDirectory = Path.GetDirectoryName(modulePath) ?? Directory.GetCurrentDirectory();
        }

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
        
        _modules.TryAdd(name, module);
        
        // Give it time to start
        await Task.Delay(1000);
        
        _logger.LogInformation("Module '{Name}' started on port {Port} (PID: {ProcessId}), Parent PID: {ParentPID}", 
            name, port, process.Id, parentProcessId);
        
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
                
                _modules.TryRemove(module.Name, out _);
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
                _modules.TryRemove(module.Name, out _);
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
        _modules.TryRemove(module.Name, out _);

        // Start it again
        await Task.Delay(500);
        
        var modulePath = FindModulePath(name);
        if (modulePath == null)
        {
            throw new FileNotFoundException($"Could not find module '{name}'.");
        }

        // Get the current process ID to pass to the child module
        var parentProcessId = Environment.ProcessId;

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{modulePath}\" --name {name} --port {port} --mainappurl {_mainAppUrl}",
            UseShellExecute = false,
            CreateNoWindow = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(modulePath) ?? Directory.GetCurrentDirectory()
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
            _modules.TryAdd(name, newModule);
            
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
        
        _modules.TryRemove(module.Name, out _);
        return true;
    }

    public async Task StopAllModulesAsync()
    {
        _logger.LogInformation("Stopping all modules...");
        foreach (var module in _modules.Values.ToList())
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
