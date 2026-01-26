using System.Text.Json;

namespace DataStore.Providers.FileSystem;

/// <summary>
/// File system-based data provider implementation.
/// Stores data as JSON files in a local directory structure.
/// </summary>
public class FileSystemDataProvider : IDataProvider
{
    private readonly string _basePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public string ProviderName => "FileSystem";

    public FileSystemDataProvider(string? basePath = null)
    {
        _basePath = basePath ?? Path.Combine(AppContext.BaseDirectory, "FileSystemData");
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Ensure base directory exists
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public Task<bool> IsAvailableAsync()
    {
        // FileSystem provider is always available
        return Task.FromResult(true);
    }

    public async Task<object> CreateAsync(string containerName, object data)
    {
        var containerPath = GetContainerPath(containerName);
        EnsureContainerExists(containerPath);

        var id = Guid.NewGuid().ToString();
        var dataWithId = new Dictionary<string, object>
        {
            ["id"] = id,
            ["data"] = data
        };

        var filePath = Path.Combine(containerPath, $"{id}.json");
        var json = JsonSerializer.Serialize(dataWithId, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json);

        return dataWithId;
    }

    public async Task<object?> GetByIdAsync(string containerName, string id)
    {
        var filePath = GetFilePath(containerName, id);
        
        if (!File.Exists(filePath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<object>(json, _jsonOptions);
    }

    public async Task<IEnumerable<object>> GetAllAsync(string containerName)
    {
        var containerPath = GetContainerPath(containerName);
        
        if (!Directory.Exists(containerPath))
        {
            return Enumerable.Empty<object>();
        }

        var files = Directory.GetFiles(containerPath, "*.json");
        var items = new List<object>();

        foreach (var file in files)
        {
            var json = await File.ReadAllTextAsync(file);
            var item = JsonSerializer.Deserialize<object>(json, _jsonOptions);
            if (item != null)
            {
                items.Add(item);
            }
        }

        return items;
    }

    public async Task<object?> UpdateAsync(string containerName, string id, object data)
    {
        var filePath = GetFilePath(containerName, id);
        
        if (!File.Exists(filePath))
        {
            return null;
        }

        var dataWithId = new Dictionary<string, object>
        {
            ["id"] = id,
            ["data"] = data
        };

        var json = JsonSerializer.Serialize(dataWithId, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json);

        return dataWithId;
    }

    public Task<bool> DeleteAsync(string containerName, string id)
    {
        var filePath = GetFilePath(containerName, id);
        
        if (!File.Exists(filePath))
        {
            return Task.FromResult(false);
        }

        File.Delete(filePath);
        return Task.FromResult(true);
    }

    private string GetContainerPath(string containerName)
    {
        return Path.Combine(_basePath, containerName);
    }

    private string GetFilePath(string containerName, string id)
    {
        return Path.Combine(GetContainerPath(containerName), $"{id}.json");
    }

    private void EnsureContainerExists(string containerPath)
    {
        if (!Directory.Exists(containerPath))
        {
            Directory.CreateDirectory(containerPath);
        }
    }
}
