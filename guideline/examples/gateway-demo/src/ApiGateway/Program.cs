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
        "/api/v1/posts/* -> forum-service:5003",
        "/api/v1/comments/* -> forum-service:5003",
        "/api/v1/events/* -> forum-service:5003",
        "/api/v1/categories/* -> forum-service:5003",
        "/api/v1/reports/* -> forum-service:5003",
        "/api/v1/bookmarks/* -> forum-service:5003",
        "/hubs/forum/* -> forum-service:5003 (SignalR)",
        "/api/v1/ai/* -> ai-service:5004",
        "/api/v1/admin/* -> admin-service:5005",
        "/api/v1/notifications/* -> notification-service:5006",
        "/hubs/notifications/* -> notification-service:5006 (SignalR)"
    }
}));

// Map reverse proxy pipeline
app.MapReverseProxy();

app.Run();

namespace ApiGateway
{
    public partial class Program { }
}
