var builder = WebApplication.CreateBuilder(args);

// 1. Add CORS for Frontend SPA/Mobile clients
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 2. Register YARP Reverse Proxy from appsettings.json
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseCors("AllowAll");

// Health probe endpoint
app.MapHealthChecks("/health");

// Root information endpoint
app.MapGet("/", () => Results.Ok(new
{
    gateway = "FPT Alumni Connect — YARP API Gateway",
    status = "Healthy",
    version = "1.0.0",
    routes = new[]
    {
        "/api/v1/users/* -> user-service:5001",
        "/api/v1/auth/* -> user-service:5001",
        "/api/v1/chats/* -> chat-service:5002",
        "/api/chat/* -> chat-service:5002",
        "/hubs/chat/* -> chat-service:5002 (SignalR)",
        "/api/v1/admin/* -> admin-service:5005"
    }
}));

// Map reverse proxy pipeline
app.MapReverseProxy();

app.Run();

namespace ApiGateway
{
    public partial class Program { }
}
