using ContainerDetectionModule.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register detection services as singletons
builder.Services.AddSingleton<DetectionService>();
builder.Services.AddSingleton<BuiltInVisionService>();
builder.Services.AddSingleton<FoundryVisionService>();

var app = builder.Build();

// Configure the HTTP request pipeline - Enable OpenAPI and Scalar UI
app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
