using System.Net;
using System.Net.Http.Json;
using Contracts.Grpc.UserProfile.V1;
using ChatRestApi.Models;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UserGrpcServer.Services;
using Xunit;

namespace GrpcDemo.Tests;

public class ProfileLookupIntegrationTests : IClassFixture<WebApplicationFactory<UserGrpcServer.Program>>
{
    private readonly WebApplicationFactory<UserGrpcServer.Program> _userServerFactory;

    public ProfileLookupIntegrationTests(WebApplicationFactory<UserGrpcServer.Program> userServerFactory)
    {
        _userServerFactory = userServerFactory;
    }

    [Fact]
    public async Task DirectGrpcService_KnownUserId_ReturnsExpectedProfileSummary()
    {
        // Arrange
        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        var logger = loggerFactory.CreateLogger<UserProfileGrpcService>();
        var service = new UserProfileGrpcService(logger);

        var knownUserId = "11111111-1111-1111-1111-111111111111";
        var request = new GetUserSummaryRequest { UserId = knownUserId };

        // Act
        var response = await service.GetUserSummary(request, new TestServerCallContext());

        // Assert
        Assert.NotNull(response);
        Assert.Equal(knownUserId, response.UserId);
        Assert.Equal("Nguyen Van A", response.DisplayName);
        Assert.Equal("https://cdn.falumni.local/avatars/nguyenvana.jpg", response.AvatarUrl);
        Assert.Equal("Alumni", response.Status);
    }

    [Fact]
    public async Task DirectGrpcService_UnknownUserId_ThrowsRpcExceptionNotFound()
    {
        // Arrange
        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        var logger = loggerFactory.CreateLogger<UserProfileGrpcService>();
        var service = new UserProfileGrpcService(logger);

        var unknownUserId = Guid.NewGuid().ToString();
        var request = new GetUserSummaryRequest { UserId = unknownUserId };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            service.GetUserSummary(request, new TestServerCallContext()));

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
        Assert.Contains(unknownUserId, ex.Status.Detail);
    }

    [Fact]
    public async Task DirectGrpcService_InvalidUserId_ThrowsRpcExceptionInvalidArgument()
    {
        // Arrange
        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        var logger = loggerFactory.CreateLogger<UserProfileGrpcService>();
        var service = new UserProfileGrpcService(logger);

        var request = new GetUserSummaryRequest { UserId = "invalid-not-a-guid" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            service.GetUserSummary(request, new TestServerCallContext()));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
    }

    [Fact]
    public async Task ChatRestApi_SuccessfulProfileLookup_Returns200WithCombinedResponse()
    {
        // Arrange: Configure ChatRestApi to call UserGrpcServer via in-memory TestServer handler
        using var chatApiFactory = new WebApplicationFactory<ChatRestApi.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    // Re-register gRPC client to use in-memory User gRPC server handler
                    services.AddGrpcClient<UserProfileService.UserProfileServiceClient>(options =>
                    {
                        options.Address = new Uri("http://localhost");
                    }).ConfigurePrimaryHttpMessageHandler(() => _userServerFactory.Server.CreateHandler());
                });
            });

        var client = chatApiFactory.CreateClient();
        var conversationId = Guid.NewGuid();
        var knownUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Act
        var response = await client.GetAsync($"/api/v1/chats/conversations/{conversationId}/participants/{knownUserId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<ConversationParticipantResponse>();
        Assert.NotNull(content);
        Assert.Equal(conversationId, content.ConversationId);
        Assert.Equal(knownUserId, content.UserId);
        Assert.Equal("Nguyen Van A", content.DisplayName);
        Assert.Equal("https://cdn.falumni.local/avatars/nguyenvana.jpg", content.AvatarUrl);
        Assert.Equal("Alumni", content.Status);
        Assert.Equal("gRPC (HTTP/2 Protobuf)", content.ResolvedVia);
    }

    [Fact]
    public async Task ChatRestApi_UserNotFound_Returns404ProblemDetails()
    {
        // Arrange
        using var chatApiFactory = new WebApplicationFactory<ChatRestApi.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddGrpcClient<UserProfileService.UserProfileServiceClient>(options =>
                    {
                        options.Address = new Uri("http://localhost");
                    }).ConfigurePrimaryHttpMessageHandler(() => _userServerFactory.Server.CreateHandler());
                });
            });

        var client = chatApiFactory.CreateClient();
        var conversationId = Guid.NewGuid();
        var unknownUserId = Guid.NewGuid(); // Not in in-memory sample profiles

        // Act
        var response = await client.GetAsync($"/api/v1/chats/conversations/{conversationId}/participants/{unknownUserId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
        Assert.Equal("Participant Profile Not Found", problem.Title);
        Assert.Contains(unknownUserId.ToString(), problem.Detail);
    }

    [Fact]
    public async Task ChatRestApi_GrpcServerUnavailable_Returns503ProblemDetails()
    {
        // Arrange: Point ChatRestApi to an unused unreachable port
        using var chatApiFactory = new WebApplicationFactory<ChatRestApi.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    // Point to unreachable localhost port
                    services.AddGrpcClient<UserProfileService.UserProfileServiceClient>(options =>
                    {
                        options.Address = new Uri("http://127.0.0.1:59998");
                    });
                });
            });

        var client = chatApiFactory.CreateClient();
        var conversationId = Guid.NewGuid();
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Act
        var response = await client.GetAsync($"/api/v1/chats/conversations/{conversationId}/participants/{userId}");

        // Assert: Maps StatusCode.Unavailable to HTTP 503
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(503, problem.Status);
        Assert.Equal("User Profile Service Unavailable", problem.Title);
    }

    [Fact]
    public async Task ChatRestApi_RequestTimeout_Returns504ProblemDetails()
    {
        // Arrange: Configure timeout to -1 seconds so deadline has already expired
        using var chatApiFactory = new WebApplicationFactory<ChatRestApi.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("GrpcServices:UserProfile:TimeoutSeconds", "-1");
                builder.ConfigureTestServices(services =>
                {
                    services.AddGrpcClient<UserProfileService.UserProfileServiceClient>(options =>
                    {
                        options.Address = new Uri("http://localhost");
                    }).ConfigurePrimaryHttpMessageHandler(() => _userServerFactory.Server.CreateHandler());
                });
            });

        var client = chatApiFactory.CreateClient();
        var conversationId = Guid.NewGuid();
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Act
        var response = await client.GetAsync($"/api/v1/chats/conversations/{conversationId}/participants/{userId}");

        // Assert: Maps StatusCode.DeadlineExceeded to HTTP 504
        Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(504, problem.Status);
        Assert.Equal("User Profile Service Timeout", problem.Title);
    }

    [Fact]
    public async Task LiveSocketNetwork_ChatRestApiCallsIndependentRunningUserGrpcServer_Succeeds()
    {
        // Arrange: Start an independent User gRPC server on a free ephemeral port with HTTP/2
        var userBuilder = WebApplication.CreateBuilder();
        userBuilder.Logging.ClearProviders(); // Clean logs during test run
        userBuilder.WebHost.ConfigureKestrel(kestrelOptions =>
        {
            kestrelOptions.Listen(System.Net.IPAddress.Loopback, 0, listenOptions =>
            {
                listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
            });
        });
        userBuilder.Services.AddGrpc();

        await using var userApp = userBuilder.Build();
        userApp.MapGrpcService<UserProfileGrpcService>();
        await userApp.StartAsync();

        var serverAddressesFeature = userApp.Services
            .GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>()
            .Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>();

        var grpcAddress = serverAddressesFeature?.Addresses.First() ?? throw new InvalidOperationException("No address found");

        // Act: Configure ChatRestApi to connect to the independent running User gRPC server over real socket
        using var chatApiFactory = new WebApplicationFactory<ChatRestApi.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("GrpcServices:UserProfile:Address", grpcAddress);
            });

        var client = chatApiFactory.CreateClient();
        var conversationId = Guid.NewGuid();
        var knownUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var response = await client.GetAsync($"/api/v1/chats/conversations/{conversationId}/participants/{knownUserId}");

        // Assert: Full round-trip over live TCP socket
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<ConversationParticipantResponse>();
        Assert.NotNull(content);
        Assert.Equal(conversationId, content.ConversationId);
        Assert.Equal(knownUserId, content.UserId);
        Assert.Equal("Tran Thi B", content.DisplayName);
        Assert.Equal("https://cdn.falumni.local/avatars/tranthib.jpg", content.AvatarUrl);
        Assert.Equal("Student", content.Status);

        await userApp.StopAsync();
    }
}

/// <summary>
/// Minimal ServerCallContext implementation for testing gRPC service implementations directly.
/// </summary>
public class TestServerCallContext : ServerCallContext
{
    protected override string MethodCore => "GetUserSummary";
    protected override string HostCore => "localhost";
    protected override string PeerCore => "127.0.0.1";
    protected override DateTime DeadlineCore => DateTime.MaxValue;
    protected override Metadata RequestHeadersCore => [];
    protected override CancellationToken CancellationTokenCore => CancellationToken.None;
    protected override Metadata ResponseTrailersCore => [];
    protected override Status StatusCore { get; set; } = Status.DefaultSuccess;
    protected override WriteOptions? WriteOptionsCore { get; set; }
    protected override AuthContext AuthContextCore => new(null, new Dictionary<string, List<AuthProperty>>());

    protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) =>
        throw new NotImplementedException();

    protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
}
