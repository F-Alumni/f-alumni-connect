using UserGrpcServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add gRPC framework services
builder.Services.AddGrpc();

var app = builder.Build();

// Map gRPC service implementation
app.MapGrpcService<UserProfileGrpcService>();
app.MapGet("/", () => "User Profile gRPC Server is active. Internal services connect via HTTP/2 Protobuf.");

app.Run();

// Required for WebApplicationFactory in integration tests
namespace UserGrpcServer
{
    public partial class Program { }
}
