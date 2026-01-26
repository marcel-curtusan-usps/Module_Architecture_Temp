using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace MainApp;

class Program
{
    private static readonly List<ModuleProcess> modules = new();
    private static readonly HttpClient httpClient = new();
    private static int nextPort = 5000;

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== MainApp - Module Management Console ===\n");

        while (true)
        {
            DisplayMenu();
            var choice = Console.ReadLine()?.Trim();

            try
            {
                switch (choice)
                {
                    case "1":
                        await StartModule();
                        break;
                    case "2":
                        await StopModule();
                        break;
                    case "3":
                        await RestartModule();
                        break;
                    case "4":
                        await KillModule();
                        break;
                    case "5":
                        await ListModules();
                        break;
                    case "6":
                        await PerformCrud();
                        break;
                    case "7":
                        await CheckHealth();
                        break;
                    case "8":
                        await StopAllModules();
                        Console.WriteLine("\nExiting MainApp...");
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
            Console.Clear();
        }
    }

    static void DisplayMenu()
    {
        Console.WriteLine("\n=== Main Menu ===");
        Console.WriteLine("1. Start Module");
        Console.WriteLine("2. Stop Module (Graceful)");
        Console.WriteLine("3. Restart Module");
        Console.WriteLine("4. Kill Module (Force)");
        Console.WriteLine("5. List Modules");
        Console.WriteLine("6. Perform CRUD Operations");
        Console.WriteLine("7. Check Module Health");
        Console.WriteLine("8. Exit (Stop All Modules)");
        Console.Write("\nEnter choice: ");
    }

    static async Task StartModule()
    {
        Console.Write("Enter module name: ");
        var name = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            Console.WriteLine("Invalid name.");
            return;
        }

        var port = nextPort++;
        
        // Find the ModuleApi.dll path
        var moduleApiPath = FindModuleApiPath();
        if (moduleApiPath == null)
        {
            Console.WriteLine("Error: Could not find ModuleApi. Please build the solution first.");
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{moduleApiPath} --name {name} --port {port}",
            UseShellExecute = false,
            CreateNoWindow = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        var process = Process.Start(startInfo);
        if (process != null)
        {
            var module = new ModuleProcess
            {
                Name = name,
                Port = port,
                Process = process,
                StartTime = DateTime.Now
            };
            modules.Add(module);
            
            // Give it time to start
            await Task.Delay(1000);
            
            Console.WriteLine($"Module '{name}' started on port {port} (PID: {process.Id})");
        }
        else
        {
            Console.WriteLine("Failed to start module.");
        }
    }

    static string? FindModuleApiPath()
    {
        // Look for ModuleApi.dll in common build locations
        var basePath = Directory.GetCurrentDirectory();
        var possiblePaths = new[]
        {
            Path.Combine(basePath, "..", "ModuleApi", "bin", "Debug", "net10.0", "ModuleApi.dll"),
            Path.Combine(basePath, "..", "ModuleApi", "bin", "Release", "net10.0", "ModuleApi.dll"),
            Path.Combine(basePath, "ModuleApi", "bin", "Debug", "net10.0", "ModuleApi.dll"),
            Path.Combine(basePath, "ModuleApi", "bin", "Release", "net10.0", "ModuleApi.dll")
        };

        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
                return fullPath;
        }

        return null;
    }

    static async Task StopModule()
    {
        var module = await SelectModule();
        if (module == null) return;

        try
        {
            var response = await httpClient.PostAsync($"http://localhost:{module.Port}/stop", null);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Stop request sent to module '{module.Name}'. Waiting for graceful shutdown...");
                
                if (!module.Process.WaitForExit(5000))
                {
                    Console.WriteLine("Module did not stop in time. Force killing...");
                    module.Process.Kill();
                }
                
                modules.Remove(module);
                Console.WriteLine($"Module '{module.Name}' stopped.");
            }
            else
            {
                Console.WriteLine($"Failed to stop module: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping module: {ex.Message}");
            Console.WriteLine("Attempting force kill...");
            if (!module.Process.HasExited)
            {
                module.Process.Kill();
                modules.Remove(module);
            }
        }
    }

    static async Task RestartModule()
    {
        var module = await SelectModule();
        if (module == null) return;

        var name = module.Name;
        var port = module.Port;

        Console.WriteLine($"Restarting module '{name}'...");
        
        // Stop the module
        try
        {
            await httpClient.PostAsync($"http://localhost:{port}/stop", null);
            module.Process.WaitForExit(3000);
        }
        catch { }

        if (!module.Process.HasExited)
        {
            module.Process.Kill();
        }
        modules.Remove(module);

        // Start it again
        await Task.Delay(500);
        
        var moduleApiPath = FindModuleApiPath();
        if (moduleApiPath == null)
        {
            Console.WriteLine("Error: Could not find ModuleApi.");
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{moduleApiPath} --name {name} --port {port}",
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
            modules.Add(newModule);
            
            await Task.Delay(1000);
            Console.WriteLine($"Module '{name}' restarted on port {port}");
        }
    }

    static async Task KillModule()
    {
        var module = await SelectModule();
        if (module == null) return;

        if (!module.Process.HasExited)
        {
            module.Process.Kill();
            Console.WriteLine($"Module '{module.Name}' force killed.");
        }
        else
        {
            Console.WriteLine($"Module '{module.Name}' is already stopped.");
        }
        modules.Remove(module);
    }

    static Task ListModules()
    {
        Console.WriteLine("\n=== Active Modules ===");
        if (modules.Count == 0)
        {
            Console.WriteLine("No modules running.");
            return Task.CompletedTask;
        }

        for (int i = 0; i < modules.Count; i++)
        {
            var module = modules[i];
            var status = module.Process.HasExited ? "Stopped" : "Running";
            var uptime = DateTime.Now - module.StartTime;
            Console.WriteLine($"{i + 1}. Name: {module.Name}, Port: {module.Port}, PID: {module.Process.Id}, Status: {status}, Uptime: {uptime:hh\\:mm\\:ss}");
        }
        return Task.CompletedTask;
    }

    static async Task PerformCrud()
    {
        var module = await SelectModule();
        if (module == null) return;

        while (true)
        {
            Console.WriteLine($"\n=== CRUD Operations for '{module.Name}' (Port: {module.Port}) ===");
            Console.WriteLine("1. List all items");
            Console.WriteLine("2. Get item by ID");
            Console.WriteLine("3. Add item");
            Console.WriteLine("4. Update item");
            Console.WriteLine("5. Delete item");
            Console.WriteLine("6. Back to main menu");
            Console.Write("Enter choice: ");

            var choice = Console.ReadLine()?.Trim();
            var baseUrl = $"http://localhost:{module.Port}";

            try
            {
                switch (choice)
                {
                    case "1":
                        var items = await httpClient.GetFromJsonAsync<List<Item>>($"{baseUrl}/items");
                        Console.WriteLine("\nItems:");
                        if (items?.Count > 0)
                        {
                            foreach (var item in items)
                            {
                                Console.WriteLine($"  ID: {item.Id}, Name: {item.Name}, Description: {item.Description}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("  No items found.");
                        }
                        break;

                    case "2":
                        Console.Write("Enter item ID: ");
                        if (int.TryParse(Console.ReadLine(), out var getId))
                        {
                            var response = await httpClient.GetAsync($"{baseUrl}/items/{getId}");
                            if (response.IsSuccessStatusCode)
                            {
                                var item = await response.Content.ReadFromJsonAsync<Item>();
                                Console.WriteLine($"Item: ID={item?.Id}, Name={item?.Name}, Description={item?.Description}");
                            }
                            else
                            {
                                Console.WriteLine($"Item not found or error: {response.StatusCode}");
                            }
                        }
                        break;

                    case "3":
                        Console.Write("Enter item name: ");
                        var name = Console.ReadLine()?.Trim();
                        Console.Write("Enter item description: ");
                        var desc = Console.ReadLine()?.Trim();
                        var createDto = new { Name = name, Description = desc };
                        var createResponse = await httpClient.PostAsJsonAsync($"{baseUrl}/items", createDto);
                        if (createResponse.IsSuccessStatusCode)
                        {
                            var created = await createResponse.Content.ReadFromJsonAsync<Item>();
                            Console.WriteLine($"Item created: ID={created?.Id}");
                        }
                        else
                        {
                            Console.WriteLine($"Failed to create item: {createResponse.StatusCode}");
                        }
                        break;

                    case "4":
                        Console.Write("Enter item ID to update: ");
                        if (int.TryParse(Console.ReadLine(), out var updateId))
                        {
                            Console.Write("Enter new name: ");
                            var updateName = Console.ReadLine()?.Trim();
                            Console.Write("Enter new description: ");
                            var updateDesc = Console.ReadLine()?.Trim();
                            var updateDto = new { Name = updateName, Description = updateDesc };
                            var updateResponse = await httpClient.PutAsJsonAsync($"{baseUrl}/items/{updateId}", updateDto);
                            if (updateResponse.IsSuccessStatusCode)
                            {
                                Console.WriteLine("Item updated successfully.");
                            }
                            else
                            {
                                Console.WriteLine($"Failed to update item: {updateResponse.StatusCode}");
                            }
                        }
                        break;

                    case "5":
                        Console.Write("Enter item ID to delete: ");
                        if (int.TryParse(Console.ReadLine(), out var deleteId))
                        {
                            var deleteResponse = await httpClient.DeleteAsync($"{baseUrl}/items/{deleteId}");
                            if (deleteResponse.IsSuccessStatusCode)
                            {
                                Console.WriteLine("Item deleted successfully.");
                            }
                            else
                            {
                                Console.WriteLine($"Failed to delete item: {deleteResponse.StatusCode}");
                            }
                        }
                        break;

                    case "6":
                        return;

                    default:
                        Console.WriteLine("Invalid choice.");
                        break;
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Error: {ex.Message}");
                Console.WriteLine("The module may not be responding. Please check if it's still running.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }

    static async Task CheckHealth()
    {
        Console.WriteLine("\n=== Module Health Status ===");
        if (modules.Count == 0)
        {
            Console.WriteLine("No modules running.");
            return;
        }

        foreach (var module in modules)
        {
            try
            {
                var response = await httpClient.GetAsync($"http://localhost:{module.Port}/health");
                if (response.IsSuccessStatusCode)
                {
                    var health = await response.Content.ReadFromJsonAsync<JsonElement>();
                    var status = health.GetProperty("status").GetString();
                    var timestamp = health.GetProperty("timestamp").GetDateTime();
                    Console.WriteLine($"Module '{module.Name}' (Port {module.Port}): {status} - Last check: {timestamp:HH:mm:ss}");
                }
                else
                {
                    Console.WriteLine($"Module '{module.Name}' (Port {module.Port}): Unhealthy - HTTP {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Module '{module.Name}' (Port {module.Port}): Unreachable - {ex.Message}");
            }
        }
    }

    static async Task StopAllModules()
    {
        Console.WriteLine("Stopping all modules...");
        foreach (var module in modules.ToList())
        {
            try
            {
                await httpClient.PostAsync($"http://localhost:{module.Port}/stop", null);
                module.Process.WaitForExit(2000);
            }
            catch { }

            if (!module.Process.HasExited)
            {
                module.Process.Kill();
            }
        }
        modules.Clear();
        Console.WriteLine("All modules stopped.");
    }

    static Task<ModuleProcess?> SelectModule()
    {
        if (modules.Count == 0)
        {
            Console.WriteLine("No modules running.");
            return Task.FromResult<ModuleProcess?>(null);
        }

        Console.WriteLine("\nSelect a module:");
        for (int i = 0; i < modules.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {modules[i].Name} (Port: {modules[i].Port})");
        }
        Console.Write("Enter module number: ");
        
        if (int.TryParse(Console.ReadLine(), out var choice) && choice > 0 && choice <= modules.Count)
        {
            return Task.FromResult<ModuleProcess?>(modules[choice - 1]);
        }

        Console.WriteLine("Invalid selection.");
        return Task.FromResult<ModuleProcess?>(null);
    }
}

class ModuleProcess
{
    public required string Name { get; set; }
    public required int Port { get; set; }
    public required Process Process { get; set; }
    public required DateTime StartTime { get; set; }
}

record Item(int Id, string Name, string? Description);
