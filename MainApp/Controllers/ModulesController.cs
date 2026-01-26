using MainApp.Models;
using MainApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace MainApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ModulesController : ControllerBase
{
    private readonly ModuleManagerService _moduleManager;
    private readonly ILogger<ModulesController> _logger;

    public ModulesController(ModuleManagerService moduleManager, ILogger<ModulesController> logger)
    {
        _moduleManager = moduleManager;
        _logger = logger;
    }

    /// <summary>
    /// Get list of all modules with their status
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ModuleDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<ModuleDto>> GetModules()
    {
        var modules = _moduleManager.GetAllModules();
        var moduleDtos = modules.Select(m => 
        {
            try
            {
                return new ModuleDto(
                    m.Name,
                    m.Port,
                    m.Process.HasExited ? 0 : m.Process.Id,
                    m.Status,
                    m.StartTime,
                    m.Uptime
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting process info for module '{Name}'", m.Name);
                return new ModuleDto(
                    m.Name,
                    m.Port,
                    0,
                    "Error",
                    m.StartTime,
                    m.Uptime
                );
            }
        });
        
        return Ok(moduleDtos);
    }

    /// <summary>
    /// Get a specific module by name
    /// </summary>
    [HttpGet("{name}")]
    [ProducesResponseType(typeof(ModuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ModuleDto> GetModule(string name)
    {
        var module = _moduleManager.GetModuleByName(name);
        if (module == null)
        {
            return NotFound(new { message = $"Module '{name}' not found." });
        }

        try
        {
            var dto = new ModuleDto(
                module.Name,
                module.Port,
                module.Process.HasExited ? 0 : module.Process.Id,
                module.Status,
                module.StartTime,
                module.Uptime
            );
            
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting process info for module '{Name}'", name);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "Error retrieving module information." });
        }
    }

    /// <summary>
    /// Start a new module with the specified name
    /// </summary>
    [HttpPost("start")]
    [ProducesResponseType(typeof(ModuleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ModuleDto>> StartModule([FromBody] ModuleStartRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Module name is required." });
        }

        try
        {
            var module = await _moduleManager.StartModuleAsync(request.Name);
            
            try
            {
                var dto = new ModuleDto(
                    module.Name,
                    module.Port,
                    module.Process.HasExited ? 0 : module.Process.Id,
                    module.Status,
                    module.StartTime,
                    module.Uptime
                );
                
                return CreatedAtAction(nameof(GetModule), new { name = module.Name }, dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting process info for newly started module '{Name}'", request.Name);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "Module started but unable to retrieve full information." });
            }
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (FileNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Stop a module gracefully
    /// </summary>
    [HttpPost("{name}/stop")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StopModule(string name)
    {
        var success = await _moduleManager.StopModuleAsync(name);
        if (!success)
        {
            return NotFound(new { message = $"Module '{name}' not found." });
        }

        return Ok(new { message = $"Module '{name}' stopped successfully." });
    }

    /// <summary>
    /// Restart a module
    /// </summary>
    [HttpPost("{name}/restart")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestartModule(string name)
    {
        var success = await _moduleManager.RestartModuleAsync(name);
        if (!success)
        {
            return NotFound(new { message = $"Module '{name}' not found." });
        }

        return Ok(new { message = $"Module '{name}' restarted successfully." });
    }

    /// <summary>
    /// Kill a module forcefully
    /// </summary>
    [HttpPost("{name}/kill")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult KillModule(string name)
    {
        var success = _moduleManager.KillModule(name);
        if (!success)
        {
            return NotFound(new { message = $"Module '{name}' not found." });
        }

        return Ok(new { message = $"Module '{name}' killed successfully." });
    }
}
