namespace MainApp.Models;

public class ModuleConfiguration
{
    public List<ModuleStartupSettings> StartupModules { get; set; } = new();
    public string? DefaultModule { get; set; }
}
