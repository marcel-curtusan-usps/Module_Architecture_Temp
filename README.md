# Module_Architecture_Temp

A .NET 10 solution demonstrating a modular architecture with process management and inter-process communication.

## Projects

### MainApp
An interactive console application that manages multiple ModuleApi worker processes. Features include:
- Start, stop, restart, and kill module processes
- Track multiple modules with unique ports
- Perform CRUD operations via HTTP to each module
- Health monitoring and status tracking
- Menu-driven interface

### ModuleApi
A minimal ASP.NET Core Web API that provides:
- CRUD endpoints for managing items (`/items`)
- Health check endpoint (`/health`)
- Graceful shutdown endpoint (`/stop`)
- Command-line argument support for port and name configuration

## Building the Solution

```bash
# Build both projects
dotnet build

# Build in Release mode
dotnet build --configuration Release
```

## Running the Applications

### Option 1: Using MainApp (Recommended)

1. Build the solution first:
   ```bash
   dotnet build
   ```

2. Run MainApp:
   ```bash
   cd MainApp
   dotnet run
   ```

3. Use the interactive menu to:
   - Start modules with custom names
   - Manage running modules
   - Perform CRUD operations
   - Monitor health status

### Option 2: Running ModuleApi Standalone

```bash
cd ModuleApi
dotnet run -- --name MyModule --port 5000
```

Test the API endpoints:
```bash
# Health check
curl http://localhost:5000/health

# Create an item
curl -X POST http://localhost:5000/items \
  -H "Content-Type: application/json" \
  -d '{"name":"Test Item","description":"A test"}'

# List all items
curl http://localhost:5000/items

# Update an item
curl -X PUT http://localhost:5000/items/1 \
  -H "Content-Type: application/json" \
  -d '{"name":"Updated","description":"Updated description"}'

# Delete an item
curl -X DELETE http://localhost:5000/items/1

# Graceful shutdown
curl -X POST http://localhost:5000/stop
```

## Architecture

- **MainApp** acts as a process manager that launches and manages ModuleApi instances
- Each ModuleApi instance runs on a unique port (starting from 5000)
- Communication between MainApp and ModuleApi is via HTTP/REST
- MainApp uses `System.Diagnostics.Process` to manage worker processes
- ModuleApi stores items in memory using `ConcurrentDictionary`

## Features Demonstrated

1. **Process Management**: Starting, stopping, restarting, and killing processes
2. **Inter-Process Communication**: HTTP-based CRUD operations
3. **Health Monitoring**: Real-time health checks of worker processes
4. **Graceful Shutdown**: Coordinated shutdown via `/stop` endpoint
5. **Command-Line Arguments**: Dynamic configuration via argv
6. **Multi-Instance Support**: Multiple workers on different ports