namespace DataStore.Providers.RavenDb;

/// <summary>
/// RavenDB data provider implementation.
/// TODO: Implement using RavenDB.Client package.
/// </summary>
public class RavenDbDataProvider : IDataProvider
{
    public string ProviderName => "RavenDB";

    public Task<bool> IsAvailableAsync()
    {
        // TODO: Check RavenDB connection
        return Task.FromResult(false);
    }

    public Task<object> CreateAsync(string containerName, object data)
    {
        throw new NotImplementedException("RavenDB provider not yet implemented");
    }

    public Task<object?> GetByIdAsync(string containerName, string id)
    {
        throw new NotImplementedException("RavenDB provider not yet implemented");
    }

    public Task<IEnumerable<object>> GetAllAsync(string containerName)
    {
        throw new NotImplementedException("RavenDB provider not yet implemented");
    }

    public Task<object?> UpdateAsync(string containerName, string id, object data)
    {
        throw new NotImplementedException("RavenDB provider not yet implemented");
    }

    public Task<bool> DeleteAsync(string containerName, string id)
    {
        throw new NotImplementedException("RavenDB provider not yet implemented");
    }
}
