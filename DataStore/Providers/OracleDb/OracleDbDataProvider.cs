namespace DataStore.Providers.OracleDb;

/// <summary>
/// Oracle Database data provider implementation.
/// TODO: Implement using Oracle.ManagedDataAccess.Core package.
/// </summary>
public class OracleDbDataProvider : IDataProvider
{
    public string ProviderName => "OracleDB";

    public Task<bool> IsAvailableAsync()
    {
        // TODO: Check Oracle DB connection
        return Task.FromResult(false);
    }

    public Task<object> CreateAsync(string containerName, object data)
    {
        throw new NotImplementedException("Oracle DB provider not yet implemented");
    }

    public Task<object?> GetByIdAsync(string containerName, string id)
    {
        throw new NotImplementedException("Oracle DB provider not yet implemented");
    }

    public Task<IEnumerable<object>> GetAllAsync(string containerName)
    {
        throw new NotImplementedException("Oracle DB provider not yet implemented");
    }

    public Task<object?> UpdateAsync(string containerName, string id, object data)
    {
        throw new NotImplementedException("Oracle DB provider not yet implemented");
    }

    public Task<bool> DeleteAsync(string containerName, string id)
    {
        throw new NotImplementedException("Oracle DB provider not yet implemented");
    }
}
