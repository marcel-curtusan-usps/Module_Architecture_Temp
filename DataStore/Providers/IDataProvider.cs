namespace DataStore.Providers;

/// <summary>
/// Interface for data providers that support CRUD operations on containers.
/// </summary>
public interface IDataProvider
{
    /// <summary>
    /// Gets the provider name.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Checks if this provider is available and can be used.
    /// </summary>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Creates a new item in the specified container.
    /// </summary>
    Task<object> CreateAsync(string containerName, object data);

    /// <summary>
    /// Retrieves an item by ID from the specified container.
    /// </summary>
    Task<object?> GetByIdAsync(string containerName, string id);

    /// <summary>
    /// Retrieves all items from the specified container.
    /// </summary>
    Task<IEnumerable<object>> GetAllAsync(string containerName);

    /// <summary>
    /// Updates an existing item in the specified container.
    /// </summary>
    Task<object?> UpdateAsync(string containerName, string id, object data);

    /// <summary>
    /// Deletes an item by ID from the specified container.
    /// </summary>
    Task<bool> DeleteAsync(string containerName, string id);
}
