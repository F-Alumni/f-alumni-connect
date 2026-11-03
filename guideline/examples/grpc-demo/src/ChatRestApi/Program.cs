using Contracts.Grpc.UserProfile.V1;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Register strongly-typed gRPC client for UserProfileService
builder.Services.AddGrpcClient<UserProfileService.UserProfileServiceClient>((sp, options) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var address = config["GrpcServices:UserProfile:Address"] ?? "http://localhost:50051";
    options.Address = new Uri(address);
});

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();

app.Run();

// Required for WebApplicationFactory in integration tests
namespace ChatRestApi
{
    public partial class Program { }
}
