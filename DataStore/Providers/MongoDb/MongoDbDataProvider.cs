namespace DataStore.Providers.MongoDb;

/// <summary>
/// MongoDB data provider implementation.
/// TODO: Implement using MongoDB.Driver package.
/// </summary>
public class MongoDbDataProvider : IDataProvider
{
    public string ProviderName => "MongoDB";

    public Task<bool> IsAvailableAsync()
    {
        // TODO: Check MongoDB connection
        return Task.FromResult(false);
    }

    public Task<object> CreateAsync(string containerName, object data)
    {
        throw new NotImplementedException("MongoDB provider not yet implemented");
    }

    public Task<object?> GetByIdAsync(string containerName, string id)
    {
        throw new NotImplementedException("MongoDB provider not yet implemented");
    }

    public Task<IEnumerable<object>> GetAllAsync(string containerName)
    {
        throw new NotImplementedException("MongoDB provider not yet implemented");
    }

    public Task<object?> UpdateAsync(string containerName, string id, object data)
    {
        throw new NotImplementedException("MongoDB provider not yet implemented");
    }

    public Task<bool> DeleteAsync(string containerName, string id)
    {
        throw new NotImplementedException("MongoDB provider not yet implemented");
    }
}
