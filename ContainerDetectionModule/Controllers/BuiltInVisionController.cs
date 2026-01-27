using Microsoft.AspNetCore.Mvc;
using ContainerDetectionModule.Models;
using ContainerDetectionModule.Services;

namespace ContainerDetectionModule.Controllers;

/// <summary>
/// Controller for .NET 10 built-in Computer Vision API integration.
/// Provides image analysis using native .NET capabilities.
/// </summary>
[ApiController]
[Route("api")]
public class BuiltInVisionController : ControllerBase
{
    private readonly BuiltInVisionService _visionService;
    private readonly ILogger<BuiltInVisionController> _logger;

    public BuiltInVisionController(BuiltInVisionService visionService, ILogger<BuiltInVisionController> logger)
    {
        _visionService = visionService;
        _logger = logger;
    }

    /// <summary>
    /// Analyzes an image using .NET 10 built-in Computer Vision APIs.
    /// Returns detected objects, tags, and image description.
    /// </summary>
    /// <param name="request">Image request containing base64-encoded image data</param>
    /// <returns>Vision analysis results including objects, tags, and description</returns>
    /// <response code="200">Analysis completed successfully</response>
    /// <response code="400">Invalid request (missing or invalid image data)</response>
    /// <response code="500">Internal server error during analysis</response>
    [HttpPost("builtin-vision")]
    [ProducesResponseType(typeof(VisionAnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VisionAnalysisResponse>> AnalyzeImage([FromBody] ImageRequest request)
    {
        try
        {
            _logger.LogInformation("POST /api/builtin-vision - Starting vision analysis");

            if (request == null || string.IsNullOrEmpty(request.Base64Image))
            {
                _logger.LogWarning("Invalid request - missing image data");
                return BadRequest(new { error = "Image data is required. Provide 'Base64Image' field." });
            }

            var result = await _visionService.AnalyzeImageAsync(request);

            if (!result.Success)
            {
                _logger.LogWarning("Vision analysis failed: {Message}", result.Message);
                return StatusCode(500, result);
            }

            _logger.LogInformation("Vision analysis completed successfully - detected {Count} objects, {TagCount} tags", 
                result.Objects.Count, result.Tags.Count);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in AnalyzeImage endpoint");
            return StatusCode(500, new { error = "An unexpected error occurred", details = ex.Message });
        }
    }

    /// <summary>
    /// Health check endpoint for the built-in vision service.
    /// </summary>
    /// <returns>Service health status</returns>
    [HttpGet("builtin-vision/health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> HealthCheck()
    {
        return Ok(new
        {
            service = "BuiltInVisionService",
            status = "healthy",
            timestamp = DateTime.UtcNow,
            message = ".NET 10 Computer Vision service is operational (stub implementation)"
        });
    }
}
