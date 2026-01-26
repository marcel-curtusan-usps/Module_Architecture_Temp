namespace MainApp.Models;

public record ModuleDto(
    string Name,
    int Port,
    int ProcessId,
    string Status,
    DateTime StartTime,
    string Uptime
);

public record ModuleStartRequest(string Name);

public record ModuleMessageDto(
    string ModuleName,
    int Port,
    string Message,
    DateTime Timestamp
);

public record HealthCheckResponse(
    string Status,
    string Name,
    int Port,
    DateTime Timestamp
);
