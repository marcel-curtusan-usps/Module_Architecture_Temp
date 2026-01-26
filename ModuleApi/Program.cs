using System.Collections.Concurrent;

// Parse command-line arguments
var moduleName = "DefaultModule";
var port = 5000;

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--name" && i + 1 < args.Length)
        moduleName = args[i + 1];
    else if (args[i] == "--port" && i + 1 < args.Length)
        port = int.Parse(args[i + 1]);
}

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to use the specified port
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(port);
});

// Add services to the container
builder.Services.AddOpenApi();

// In-memory item storage
var items = new ConcurrentDictionary<int, Item>();
var nextId = 1;

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

Console.WriteLine($"ModuleApi '{moduleName}' starting on port {port}...");

// Health endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    name = moduleName,
    port = port,
    timestamp = DateTime.UtcNow
}));

// Get all items
app.MapGet("/items", () => Results.Ok(items.Values));

// Get item by ID
app.MapGet("/items/{id:int}", (int id) =>
{
    if (items.TryGetValue(id, out var item))
        return Results.Ok(item);
    return Results.NotFound();
});

// Create new item
app.MapPost("/items", (ItemCreateDto dto) =>
{
    var id = Interlocked.Increment(ref nextId) - 1;
    var item = new Item(id, dto.Name, dto.Description);
    items[id] = item;
    return Results.Created($"/items/{id}", item);
});

// Update item
app.MapPut("/items/{id:int}", (int id, ItemCreateDto dto) =>
{
    if (!items.ContainsKey(id))
        return Results.NotFound();
    
    var item = new Item(id, dto.Name, dto.Description);
    items[id] = item;
    return Results.Ok(item);
});

// Delete item
app.MapDelete("/items/{id:int}", (int id) =>
{
    if (items.TryRemove(id, out _))
        return Results.NoContent();
    return Results.NotFound();
});

// Graceful stop endpoint
app.MapPost("/stop", async (IHostApplicationLifetime lifetime) =>
{
    Console.WriteLine($"ModuleApi '{moduleName}' received stop request. Shutting down gracefully...");
    _ = Task.Run(async () =>
    {
        await Task.Delay(500); // Small delay to allow response to be sent
        lifetime.StopApplication();
    });
    return Results.Ok(new { message = "Shutdown initiated" });
});

app.Run();

Console.WriteLine($"ModuleApi '{moduleName}' stopped.");

// Data models
record Item(int Id, string Name, string? Description);
record ItemCreateDto(string Name, string? Description);
