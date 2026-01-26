# Module_Architecture_Temp

A .NET 10 solution demonstrating a modular architecture with ASP.NET Core Web API for process management and inter-process communication.

## Projects

### MainApp (ASP.NET Core Web API)
A REST API application that manages multiple module worker processes via HTTP endpoints. Features include:
- **RESTful API endpoints** for module lifecycle management (start, stop, restart, kill, status)
- **Interactive OpenAPI documentation** with Scalar UI (accessible at root path `/`)
- **Module communication endpoints** for modules to send messages to the main app
- **Swagger/OpenAPI** specification out of the box
- **IIS-ready** configuration for production deployment
- **Graceful shutdown** handling to stop all managed modules

#### API Endpoints

**Module Management** (`/api/modules`):
- `GET /api/modules` - List all modules with their status
- `GET /api/modules/{name}` - Get specific module details
- `POST /api/modules/start` - Start a new module (body: `{"name": "ModuleName"}`)
- `POST /api/modules/{name}/stop` - Stop a module gracefully
- `POST /api/modules/{name}/restart` - Restart a module
- `POST /api/modules/{name}/kill` - Force kill a module

**Module Messages** (`/api/module-messages`):
- `POST /api/module-messages/ready` - Module reports it's ready
- `POST /api/module-messages/heartbeat` - Module sends heartbeat
- `POST /api/module-messages/error` - Module reports an error
- `POST /api/module-messages/info` - Module sends information

### DataStore (ASP.NET Core Web API)
A generic data storage API with multi-provider support and automatic fallback. Features include:
- **Generic CRUD endpoints** via `/api/data/{containerName}/[id]`
- **Multi-provider architecture** supporting MongoDB, CosmosDB, RavenDB, Oracle, and FileSystem
- **Automatic provider selection** with fallback priority (MongoDB → CosmosDB → RavenDB → Oracle → FileSystem)
- **Interactive OpenAPI documentation** with Scalar UI
- **FileSystem provider** fully implemented as working example and reliable fallback
- **Container-based storage** for organizing data into logical groups

See [DataStore/README.md](DataStore/README.md) for detailed documentation.

## Building the Solution

```bash
# Build both projects
dotnet build

# Build in Release mode
dotnet build --configuration Release
```

## Running the Applications

### Running MainApp Web API

1. Build the solution first:
   ```bash
   dotnet build
   ```

2. Run MainApp:
   ```bash
   cd MainApp
   dotnet run
   ```

3. Access the interactive API documentation at `http://localhost:5000` (or the configured port)

4. Use the API endpoints to manage modules:
   ```bash
   # Start a module
   curl -X POST http://localhost:5000/api/modules/start \
     -H "Content-Type: application/json" \
     -d '{"name":"MyModule"}'
   
   # List all modules
   curl http://localhost:5000/api/modules
   
   # Stop a module
   curl -X POST http://localhost:5000/api/modules/MyModule/stop
   ```

## Architecture

- **MainApp** is an ASP.NET Core Web API that launches and manages module instances as separate processes
- Each module instance runs on a unique port (starting from 5000)
- Communication between MainApp and modules is via HTTP/REST
- MainApp uses `System.Diagnostics.Process` to manage worker processes
- **OpenAPI/Swagger** documentation is available via Scalar UI at the application root

## Features Demonstrated

1. **REST API Architecture**: Complete REST API with controllers, services, and DTOs
2. **Process Management**: Starting, stopping, restarting, and killing processes via API
3. **Inter-Process Communication**: HTTP-based module messaging
4. **Health Monitoring**: Real-time health checks of worker processes
5. **Graceful Shutdown**: Coordinated shutdown via `/stop` endpoint and application lifecycle
6. **Command-Line Arguments**: Dynamic configuration via argv
7. **Multi-Instance Support**: Multiple workers on different ports
8. **OpenAPI Documentation**: Interactive API documentation with Scalar UI
9. **IIS Deployment Ready**: ASP.NET Core Web API ready for IIS hosting

## API Documentation

When running MainApp, navigate to the root URL (e.g., `http://localhost:5000`) to access the interactive Scalar UI documentation. This provides:
- Complete API endpoint documentation
- Request/response schemas
- Interactive "Try it out" functionality
- Code examples in multiple languages (curl, JavaScript, Python, etc.)

## Deployment

### IIS Deployment

MainApp is configured as an ASP.NET Core Web API and can be deployed to IIS:

1. Publish the application:
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. Configure IIS with the ASP.NET Core Module (ANCM)

3. Point the IIS application to the publish folder

4. Ensure the application pool identity has permissions to execute `dotnet` and manage child processes