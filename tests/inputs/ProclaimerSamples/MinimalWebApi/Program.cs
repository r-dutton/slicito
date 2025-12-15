using MinimalWebApi;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/api/minimal/widgets/{id}", MinimalHandlers.GetMinimalWidget);
app.MapPost("/api/minimal/widgets", MinimalHandlers.CreateMinimalWidget);

app.Run();
