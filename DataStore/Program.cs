using DataStore.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register the data provider selector as a singleton
builder.Services.AddSingleton<DataProviderSelector>();

var app = builder.Build();

// Configure the HTTP request pipeline - Enable OpenAPI and Scalar UI
app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Initialize the data provider on startup
var providerSelector = app.Services.GetRequiredService<DataProviderSelector>();
await providerSelector.GetProviderAsync();

app.Run();
