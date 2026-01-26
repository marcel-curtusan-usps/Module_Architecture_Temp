using Microsoft.AspNetCore.Mvc;
using DataStore.Services;
using System.Text.Json;

namespace DataStore.Controllers;

/// <summary>
/// Controller for generic CRUD operations on data containers.
/// All operations are routed through /api/data/{containerName}/[id].
/// </summary>
[ApiController]
[Route("api/data")]
public class DataController : ControllerBase
{
    private readonly DataProviderSelector _providerSelector;
    private readonly ILogger<DataController> _logger;

    public DataController(DataProviderSelector providerSelector, ILogger<DataController> logger)
    {
        _providerSelector = providerSelector;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new item in the specified container.
    /// </summary>
    /// <param name="containerName">The name of the container/collection.</param>
    /// <param name="data">The data to store.</param>
    /// <returns>The created item with its generated ID.</returns>
    [HttpPost("{containerName}")]
    public async Task<ActionResult<object>> Create(string containerName, [FromBody] JsonElement data)
    {
        try
        {
            _logger.LogInformation("Creating item in container: {ContainerName}", containerName);
            var provider = await _providerSelector.GetProviderAsync();
            var result = await provider.CreateAsync(containerName, data);
            return CreatedAtAction(nameof(GetById), new { containerName, id = GetIdFromResult(result) }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating item in container: {ContainerName}", containerName);
            return StatusCode(500, new { error = "Failed to create item", details = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves a specific item by ID from the specified container.
    /// </summary>
    /// <param name="containerName">The name of the container/collection.</param>
    /// <param name="id">The ID of the item to retrieve.</param>
    /// <returns>The item if found, otherwise 404.</returns>
    [HttpGet("{containerName}/{id}")]
    public async Task<ActionResult<object>> GetById(string containerName, string id)
    {
        try
        {
            _logger.LogInformation("Getting item {Id} from container: {ContainerName}", id, containerName);
            var provider = await _providerSelector.GetProviderAsync();
            var result = await provider.GetByIdAsync(containerName, id);
            
            if (result == null)
            {
                return NotFound(new { error = "Item not found", containerName, id });
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting item {Id} from container: {ContainerName}", id, containerName);
            return StatusCode(500, new { error = "Failed to retrieve item", details = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves all items from the specified container.
    /// </summary>
    /// <param name="containerName">The name of the container/collection.</param>
    /// <returns>A list of all items in the container.</returns>
    [HttpGet("{containerName}")]
    public async Task<ActionResult<IEnumerable<object>>> GetAll(string containerName)
    {
        try
        {
            _logger.LogInformation("Getting all items from container: {ContainerName}", containerName);
            var provider = await _providerSelector.GetProviderAsync();
            var result = await provider.GetAllAsync(containerName);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting items from container: {ContainerName}", containerName);
            return StatusCode(500, new { error = "Failed to retrieve items", details = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing item in the specified container.
    /// </summary>
    /// <param name="containerName">The name of the container/collection.</param>
    /// <param name="id">The ID of the item to update.</param>
    /// <param name="data">The updated data.</param>
    /// <returns>The updated item if found, otherwise 404.</returns>
    [HttpPut("{containerName}/{id}")]
    public async Task<ActionResult<object>> Update(string containerName, string id, [FromBody] JsonElement data)
    {
        try
        {
            _logger.LogInformation("Updating item {Id} in container: {ContainerName}", id, containerName);
            var provider = await _providerSelector.GetProviderAsync();
            var result = await provider.UpdateAsync(containerName, id, data);
            
            if (result == null)
            {
                return NotFound(new { error = "Item not found", containerName, id });
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating item {Id} in container: {ContainerName}", id, containerName);
            return StatusCode(500, new { error = "Failed to update item", details = ex.Message });
        }
    }

    /// <summary>
    /// Deletes an item from the specified container.
    /// </summary>
    /// <param name="containerName">The name of the container/collection.</param>
    /// <param name="id">The ID of the item to delete.</param>
    /// <returns>204 No Content if successful, 404 if item not found.</returns>
    [HttpDelete("{containerName}/{id}")]
    public async Task<ActionResult> Delete(string containerName, string id)
    {
        try
        {
            _logger.LogInformation("Deleting item {Id} from container: {ContainerName}", id, containerName);
            var provider = await _providerSelector.GetProviderAsync();
            var result = await provider.DeleteAsync(containerName, id);
            
            if (!result)
            {
                return NotFound(new { error = "Item not found", containerName, id });
            }
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting item {Id} from container: {ContainerName}", id, containerName);
            return StatusCode(500, new { error = "Failed to delete item", details = ex.Message });
        }
    }

    /// <summary>
    /// Gets information about the active data provider.
    /// </summary>
    /// <returns>Information about the currently active provider.</returns>
    [HttpGet("provider")]
    public async Task<ActionResult<object>> GetProviderInfo()
    {
        try
        {
            var provider = await _providerSelector.GetProviderAsync();
            return Ok(new
            {
                providerName = provider.ProviderName,
                isAvailable = await provider.IsAvailableAsync()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provider information");
            return StatusCode(500, new { error = "Failed to get provider information", details = ex.Message });
        }
    }

    private string GetIdFromResult(object result)
    {
        // Extract ID from result - assuming it's in a Dictionary format
        if (result is Dictionary<string, object> dict && dict.ContainsKey("id"))
        {
            return dict["id"].ToString() ?? string.Empty;
        }
        return string.Empty;
    }
}
