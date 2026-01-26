using MainApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace MainApp.Controllers;

[ApiController]
[Route("api/module-messages")]
public class ModuleMessagesController : ControllerBase
{
    private readonly ILogger<ModuleMessagesController> _logger;

    public ModuleMessagesController(ILogger<ModuleMessagesController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Module sends a "ready" message when it has started successfully
    /// </summary>
    [HttpPost("ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Ready([FromBody] ModuleMessageDto message)
    {
        _logger.LogInformation("Module '{ModuleName}' on port {Port} is ready at {Timestamp}", 
            message.ModuleName, message.Port, message.Timestamp);
        
        return Ok(new { message = "Ready message received", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Module sends periodic heartbeat messages
    /// </summary>
    [HttpPost("heartbeat")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Heartbeat([FromBody] ModuleMessageDto message)
    {
        _logger.LogDebug("Heartbeat received from module '{ModuleName}' on port {Port}", 
            message.ModuleName, message.Port);
        
        return Ok(new { message = "Heartbeat acknowledged", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Module sends error or warning messages
    /// </summary>
    [HttpPost("error")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Error([FromBody] ModuleMessageDto message)
    {
        _logger.LogError("Error from module '{ModuleName}' on port {Port}: {Message}", 
            message.ModuleName, message.Port, message.Message);
        
        return Ok(new { message = "Error message received", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Module sends informational messages
    /// </summary>
    [HttpPost("info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Info([FromBody] ModuleMessageDto message)
    {
        _logger.LogInformation("Info from module '{ModuleName}' on port {Port}: {Message}", 
            message.ModuleName, message.Port, message.Message);
        
        return Ok(new { message = "Info message received", timestamp = DateTime.UtcNow });
    }
}
