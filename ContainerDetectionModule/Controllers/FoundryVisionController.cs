using Microsoft.AspNetCore.Mvc;
using ContainerDetectionModule.Models;
using ContainerDetectionModule.Services;

namespace ContainerDetectionModule.Controllers;

/// <summary>
/// Controller for Microsoft Foundry Computer Vision API proxy.
/// Forwards image analysis requests to Microsoft Foundry services.
/// </summary>
[ApiController]
[Route("api")]
public class FoundryVisionController : ControllerBase
{
    private readonly FoundryVisionService _foundryService;
    private readonly ILogger<FoundryVisionController> _logger;

    public FoundryVisionController(FoundryVisionService foundryService, ILogger<FoundryVisionController> logger)
    {
        _foundryService = foundryService;
        _logger = logger;
    }

    /// <summary>
    /// Proxies image analysis request to Microsoft Foundry Computer Vision API.
    /// Provides comprehensive vision analysis including objects, scenes, OCR, and more.
    /// </summary>
    /// <param name="request">Image request containing base64-encoded image data</param>
    /// <returns>Foundry vision analysis results</returns>
    /// <response code="200">Analysis completed successfully</response>
    /// <response code="400">Invalid request (missing or invalid image data)</response>
    /// <response code="500">Internal server error or Foundry API error</response>
    [HttpPost("foundry-vision")]
    [ProducesResponseType(typeof(FoundryVisionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FoundryVisionResponse>> AnalyzeWithFoundry([FromBody] ImageRequest request)
    {
        try
        {
            _logger.LogInformation("POST /api/foundry-vision - Starting Foundry analysis");

            if (request == null || string.IsNullOrEmpty(request.Base64Image))
            {
                _logger.LogWarning("Invalid request - missing image data");
                return BadRequest(new { error = "Image data is required. Provide 'Base64Image' field." });
            }

            var result = await _foundryService.AnalyzeWithFoundryAsync(request);

            if (!result.Success)
            {
                _logger.LogWarning("Foundry analysis failed: {Message}", result.Message);
                return StatusCode(500, result);
            }

            _logger.LogInformation("Foundry analysis completed successfully");
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in AnalyzeWithFoundry endpoint");
            return StatusCode(500, new { error = "An unexpected error occurred", details = ex.Message });
        }
    }

    /// <summary>
    /// Health check endpoint for the Foundry vision service.
    /// </summary>
    /// <returns>Service health status</returns>
    [HttpGet("foundry-vision/health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> HealthCheck()
    {
        return Ok(new
        {
            service = "FoundryVisionService",
            status = "healthy",
            timestamp = DateTime.UtcNow,
            message = "Microsoft Foundry proxy service is operational (stub implementation)"
        });
    }
}
