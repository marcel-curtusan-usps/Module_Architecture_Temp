using DataStore.Providers;
using DataStore.Providers.MongoDb;
using DataStore.Providers.CosmosDb;
using DataStore.Providers.RavenDb;
using DataStore.Providers.OracleDb;
using DataStore.Providers.FileSystem;

namespace DataStore.Services;

/// <summary>
/// Service responsible for selecting and managing the active data provider.
/// Implements fallback priority: MongoDB > CosmosDB > RavenDB > Oracle > FileSystem.
/// </summary>
public class DataProviderSelector
{
    private readonly ILogger<DataProviderSelector> _logger;
    private IDataProvider? _activeProvider;
    private readonly List<IDataProvider> _providers;

    public DataProviderSelector(ILogger<DataProviderSelector> logger)
    {
        _logger = logger;
        
        // Initialize providers in fallback priority order
        _providers = new List<IDataProvider>
        {
            new MongoDbDataProvider(),
            new CosmosDbDataProvider(),
            new RavenDbDataProvider(),
            new OracleDbDataProvider(),
            new FileSystemDataProvider()
        };
    }

    /// <summary>
    /// Gets the active data provider. Selects one if not already selected.
    /// </summary>
    public async Task<IDataProvider> GetProviderAsync()
    {
        if (_activeProvider != null)
        {
            return _activeProvider;
        }

        await SelectProviderAsync();
        return _activeProvider!;
    }

    /// <summary>
    /// Selects the first available provider based on fallback priority.
    /// </summary>
    private async Task SelectProviderAsync()
    {
        _logger.LogInformation("Selecting data provider based on availability...");

        foreach (var provider in _providers)
        {
            _logger.LogDebug("Checking provider: {ProviderName}", provider.ProviderName);
            
            if (await provider.IsAvailableAsync())
            {
                _activeProvider = provider;
                
                // Log warning if falling back to FileSystem
                if (provider.ProviderName == "FileSystem")
                {
                    _logger.LogWarning(
                        "No external database provider available. Falling back to FileSystem provider. " +
                        "Data will be stored in local file system.");
                }
                else
                {
                    _logger.LogInformation("Selected data provider: {ProviderName}", provider.ProviderName);
                }
                
                return;
            }
        }

        // This should never happen since FileSystem is always available
        throw new InvalidOperationException("No data provider available, including FileSystem fallback.");
    }

    /// <summary>
    /// Forces re-selection of provider (useful for testing or configuration changes).
    /// </summary>
    public async Task RefreshProviderAsync()
    {
        _activeProvider = null;
        await SelectProviderAsync();
    }
}
