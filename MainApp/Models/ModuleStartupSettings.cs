namespace MainApp.Models;

public class ModuleStartupSettings
{
    public required string Name { get; set; }
    public required int Port { get; set; }
    public string? ProjectPath { get; set; }
}
