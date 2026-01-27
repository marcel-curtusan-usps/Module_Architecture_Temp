using Microsoft.AspNetCore.Mvc;
using ContainerDetectionModule.Models;
using ContainerDetectionModule.Services;

namespace ContainerDetectionModule.Controllers;

/// <summary>
/// Controller for custom container detection endpoints.
/// Handles detection of USPS containers, people, and vehicles using custom ML models.
/// </summary>
[ApiController]
[Route("api")]
public class DetectionController : ControllerBase
{
    private readonly DetectionService _detectionService;
    private readonly ILogger<DetectionController> _logger;

    public DetectionController(DetectionService detectionService, ILogger<DetectionController> logger)
    {
        _detectionService = detectionService;
        _logger = logger;
    }

    /// <summary>
    /// Detects USPS containers, people, and vehicles in the provided image.
    /// Accepts image data as base64 string or file upload.
    /// </summary>
    /// <param name="request">Image request containing base64-encoded image data</param>
    /// <returns>Detection results with bounding boxes and confidence scores</returns>
    /// <response code="200">Detection completed successfully</response>
    /// <response code="400">Invalid request (missing or invalid image data)</response>
    /// <response code="500">Internal server error during detection</response>
    [HttpPost("detect")]
    [ProducesResponseType(typeof(DetectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DetectionResponse>> DetectObjects([FromBody] ImageRequest request)
    {
        try
        {
            _logger.LogInformation("POST /api/detect - Starting object detection");

            if (request == null || string.IsNullOrEmpty(request.Base64Image))
            {
                _logger.LogWarning("Invalid request - missing image data");
                return BadRequest(new { error = "Image data is required. Provide 'Base64Image' field." });
            }

            var result = await _detectionService.DetectObjectsAsync(request);

            if (!result.Success)
            {
                _logger.LogWarning("Detection failed: {Message}", result.Message);
                return StatusCode(500, result);
            }

            _logger.LogInformation("Detection completed successfully - found {Count} objects", 
                result.DetectedObjects.Count);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in DetectObjects endpoint");
            return StatusCode(500, new { error = "An unexpected error occurred", details = ex.Message });
        }
    }

    /// <summary>
    /// Health check endpoint for the detection service.
    /// </summary>
    /// <returns>Service health status</returns>
    [HttpGet("detect/health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> HealthCheck()
    {
        return Ok(new
        {
            service = "DetectionService",
            status = "healthy",
            timestamp = DateTime.UtcNow,
            message = "Custom detection service is operational (stub implementation)"
        });
    }
}
