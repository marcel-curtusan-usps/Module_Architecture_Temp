# DataStore API

A generic ASP.NET Core Web API for managing data across multiple storage providers with automatic fallback.

## Overview

DataStore provides a unified CRUD API that works with multiple database providers through a provider-based architecture. It automatically selects the best available provider with a defined fallback priority.

## Features

- **Generic CRUD Endpoints**: Single API for all data operations
- **Multi-Provider Support**: MongoDB, CosmosDB, RavenDB, Oracle, and FileSystem
- **Automatic Fallback**: Falls back to FileSystem when external databases are unavailable
- **Interactive Documentation**: Swagger/OpenAPI with Scalar UI
- **Container-Based**: Organize data into logical containers (like tables/collections)

## API Endpoints

All endpoints follow the pattern `/api/data/{containerName}/[id]`:

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/data/{containerName}` | Create a new item |
| GET | `/api/data/{containerName}` | Get all items in container |
| GET | `/api/data/{containerName}/{id}` | Get a specific item |
| PUT | `/api/data/{containerName}/{id}` | Update an item |
| DELETE | `/api/data/{containerName}/{id}` | Delete an item |
| GET | `/api/data/provider` | Get active provider info |

## Provider Architecture

### Fallback Priority

1. **MongoDB** (highest priority)
2. **CosmosDB**
3. **RavenDB**
4. **Oracle DB**
5. **FileSystem** (always available fallback)

The system automatically selects the first available provider and logs a warning if it falls back to FileSystem.

### Provider Interface

All providers implement the `IDataProvider` interface:

```csharp
public interface IDataProvider
{
    string ProviderName { get; }
    Task<bool> IsAvailableAsync();
    Task<object> CreateAsync(string containerName, object data);
    Task<object?> GetByIdAsync(string containerName, string id);
    Task<IEnumerable<object>> GetAllAsync(string containerName);
    Task<object?> UpdateAsync(string containerName, string id, object data);
    Task<bool> DeleteAsync(string containerName, string id);
}
```

## Running the API

### Build
```bash
cd DataStore
dotnet build
```

### Run
```bash
dotnet run
# Or specify a custom port:
dotnet run --urls "http://localhost:5002"
```

### Access Swagger UI
Navigate to `http://localhost:5002/scalar/v1` for interactive API documentation.

## Usage Examples

### Create an Item
```bash
curl -X POST http://localhost:5002/api/data/users \
  -H "Content-Type: application/json" \
  -d '{"name":"John Doe","email":"john@example.com"}'
```

### Get All Items
```bash
curl http://localhost:5002/api/data/users
```

### Get Specific Item
```bash
curl http://localhost:5002/api/data/users/{id}
```

### Update an Item
```bash
curl -X PUT http://localhost:5002/api/data/users/{id} \
  -H "Content-Type: application/json" \
  -d '{"name":"Jane Doe","email":"jane@example.com"}'
```

### Delete an Item
```bash
curl -X DELETE http://localhost:5002/api/data/users/{id}
```

### Check Active Provider
```bash
curl http://localhost:5002/api/data/provider
```

## FileSystem Provider

The FileSystem provider is fully implemented and serves as both a working example and a reliable fallback.

### Storage Location
Data is stored in: `{AppDirectory}/FileSystemData/{containerName}/{id}.json`

### Features
- Automatic container (folder) creation
- GUID-based ID generation
- Pretty-printed JSON with camelCase naming
- Thread-safe file operations

### Data Format
```json
{
  "id": "f4d75044-c60d-4bd1-aff9-11d59301895a",
  "data": {
    "name": "John Doe",
    "email": "john@example.com"
  }
}
```

## Implementing Additional Providers

To implement a provider:

1. Create a new class in the appropriate `Providers/{ProviderName}` folder
2. Implement the `IDataProvider` interface
3. Implement `IsAvailableAsync()` to check provider availability (connection, credentials, etc.)
4. Implement all CRUD operations
5. The provider will be automatically picked up by `DataProviderSelector`

Example providers are provided as placeholders:
- `MongoDbDataProvider` - Ready for MongoDB.Driver implementation
- `CosmosDbDataProvider` - Ready for Microsoft.Azure.Cosmos implementation
- `RavenDbDataProvider` - Ready for RavenDB.Client implementation
- `OracleDbDataProvider` - Ready for Oracle.ManagedDataAccess.Core implementation

## Project Structure

```
DataStore/
├── Controllers/
│   └── DataController.cs          # Main CRUD controller
├── Providers/
│   ├── IDataProvider.cs           # Provider interface
│   ├── MongoDb/
│   │   └── MongoDbDataProvider.cs
│   ├── CosmosDb/
│   │   └── CosmosDbDataProvider.cs
│   ├── RavenDb/
│   │   └── RavenDbDataProvider.cs
│   ├── OracleDb/
│   │   └── OracleDbDataProvider.cs
│   └── FileSystem/
│       └── FileSystemDataProvider.cs
├── Services/
│   └── DataProviderSelector.cs   # Provider selection logic
├── Program.cs
├── DataStore.csproj
├── appsettings.json
└── appsettings.Development.json
```

## Configuration

The API uses standard ASP.NET Core configuration. Add provider-specific settings to `appsettings.json`:

```json
{
  "DataProviders": {
    "MongoDB": {
      "ConnectionString": "mongodb://localhost:27017",
      "DatabaseName": "datastore"
    },
    "CosmosDB": {
      "Endpoint": "https://your-account.documents.azure.com:443/",
      "Key": "your-key"
    }
  }
}
```

## Integration with MainApp

The MainApp can send all data CRUD operations to this module's API:

```csharp
// Example integration from MainApp
var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5002") };

// Create item
var response = await httpClient.PostAsJsonAsync("/api/data/users", new { name = "John", email = "john@example.com" });

// Get all items
var users = await httpClient.GetFromJsonAsync<List<object>>("/api/data/users");
```

## License

Part of the Module_Architecture_Temp solution.
