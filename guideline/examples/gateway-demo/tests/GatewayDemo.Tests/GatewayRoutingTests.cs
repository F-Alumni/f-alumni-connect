using System.Net;
using System.Net.Http.Json;
using ChatRestApi.Models;
using Contracts.Grpc.UserProfile.V1;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Yarp.ReverseProxy.Forwarder;

namespace GatewayDemo.Tests;

public class TestForwarderHttpClientFactory : IForwarderHttpClientFactory
{
    private readonly HttpMessageHandler _handler;

    public TestForwarderHttpClientFactory(HttpMessageHandler handler)
    {
        _handler = handler;
    }

    public HttpMessageInvoker CreateClient(ForwarderHttpClientContext context)
    {
        return new HttpMessageInvoker(_handler);
    }
}

public class GatewayRoutingTests : IClassFixture<WebApplicationFactory<UserGrpcServer.Program>>
{
    private readonly WebApplicationFactory<UserGrpcServer.Program> _userServerFactory;

    public GatewayRoutingTests(WebApplicationFactory<UserGrpcServer.Program> userServerFactory)
    {
        _userServerFactory = userServerFactory;
    }

    [Fact]
    public async Task Gateway_HealthProbe_Returns200Ok()
    {
        // Arrange
        using var gatewayFactory = new WebApplicationFactory<ApiGateway.Program>();
        var client = gatewayFactory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", content);
    }

    [Fact]
    public async Task Gateway_RootEndpoint_ReturnsGatewayInformation()
    {
        // Arrange
        using var gatewayFactory = new WebApplicationFactory<ApiGateway.Program>();
        var client = gatewayFactory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("FPT Alumni Connect", content);
        Assert.Contains("Healthy", content);
    }

    [Fact]
    public async Task Gateway_RoutesApiChatToChatService_TransformsPathAndReturnsCombinedProfile()
    {
        // 1. Create in-memory Chat REST API wired to User gRPC server
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

        // 2. Create in-memory YARP Gateway wired to Chat REST API handler
        using var gatewayFactory = new WebApplicationFactory<ApiGateway.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    // Use custom client factory to route YARP requests through in-memory test handler
                    services.AddSingleton<IForwarderHttpClientFactory>(
                        new TestForwarderHttpClientFactory(chatApiFactory.Server.CreateHandler()));
                });
            });

        var client = gatewayFactory.CreateClient();
        var conversationId = Guid.NewGuid();
        var knownUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Act: Call gateway using the transformed route "/api/chat/..."
        var response = await client.GetAsync($"/api/chat/conversations/{conversationId}/participants/{knownUserId}");

        // Assert: 200 OK received with profile data resolved via gRPC
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<ConversationParticipantResponse>();
        Assert.NotNull(content);
        Assert.Equal(conversationId, content.ConversationId);
        Assert.Equal(knownUserId, content.UserId);
        Assert.Equal("Nguyen Van A", content.DisplayName);
        Assert.Equal("https://cdn.falumni.local/avatars/nguyenvana.jpg", content.AvatarUrl);
        Assert.Equal("Alumni", content.Status);
    }

    [Fact]
    public async Task Gateway_RoutesApiV1ChatsDirectly_Returns200Ok()
    {
        // 1. Create in-memory Chat REST API wired to User gRPC server
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

        // 2. Create in-memory YARP Gateway wired to Chat REST API handler
        using var gatewayFactory = new WebApplicationFactory<ApiGateway.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton<IForwarderHttpClientFactory>(
                        new TestForwarderHttpClientFactory(chatApiFactory.Server.CreateHandler()));
                });
            });

        var client = gatewayFactory.CreateClient();
        var conversationId = Guid.NewGuid();
        var knownUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        // Act: Call gateway using direct v1 route "/api/v1/chats/..."
        var response = await client.GetAsync($"/api/v1/chats/conversations/{conversationId}/participants/{knownUserId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<ConversationParticipantResponse>();
        Assert.NotNull(content);
        Assert.Equal(knownUserId, content.UserId);
        Assert.Equal("Tran Thi B", content.DisplayName);
        Assert.Equal("Student", content.Status);
    }

    [Fact]
    public async Task Gateway_UserNotFound_Propagates404ProblemDetails()
    {
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

        using var gatewayFactory = new WebApplicationFactory<ApiGateway.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton<IForwarderHttpClientFactory>(
                        new TestForwarderHttpClientFactory(chatApiFactory.Server.CreateHandler()));
                });
            });

        var client = gatewayFactory.CreateClient();
        var unknownUserId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/chat/conversations/{Guid.NewGuid()}/participants/{unknownUserId}");

        // Assert: 404 ProblemDetails propagated through YARP gateway
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
        Assert.Equal("Participant Profile Not Found", problem.Title);
    }

    [Fact]
    public async Task Gateway_WhenUpstreamUnavailable_Returns502Or503ProblemDetails()
    {
        // Configure YARP with an unreachable destination
        using var gatewayFactory = new WebApplicationFactory<ApiGateway.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting(
                    "ReverseProxy:Clusters:chat-cluster:Destinations:chat-service:Address",
                    "http://127.0.0.1:59997");
            });

        var client = gatewayFactory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/chat/conversations/{Guid.NewGuid()}/participants/{Guid.NewGuid()}");

        // Assert: Gateway returns 502/503 when destination cannot be reached
        Assert.True(
            response.StatusCode == HttpStatusCode.BadGateway || response.StatusCode == HttpStatusCode.ServiceUnavailable,
            $"Expected 502 BadGateway or 503 ServiceUnavailable, got {response.StatusCode}");
    }

    [Fact]
    public async Task Gateway_AdminRoute_WhenUpstreamUnavailable_Returns502Or503ProblemDetails()
    {
        // Configure YARP with an unreachable destination for admin-cluster
        using var gatewayFactory = new WebApplicationFactory<ApiGateway.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting(
                    "ReverseProxy:Clusters:admin-cluster:Destinations:admin-service:Address",
                    "http://127.0.0.1:59996");
            });

        var client = gatewayFactory.CreateClient();

        // Act: Call admin route through gateway
        var response = await client.GetAsync("/api/v1/admin/dashboard/summary");

        // Assert: YARP matches route and attempts forwarding to admin-cluster
        Assert.True(
            response.StatusCode == HttpStatusCode.BadGateway || response.StatusCode == HttpStatusCode.ServiceUnavailable,
            $"Expected 502 BadGateway or 503 ServiceUnavailable, got {response.StatusCode}");
    }
}
