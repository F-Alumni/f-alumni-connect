using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using UserService.Api.Data;
using UserService.Api.Grpc;
using UserService.Api.Middlewares;
using UserService.Services.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Logging & Configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// 2. Add Framework Services (Controllers, gRPC, CORS, OpenAPI, HealthChecks)
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddGrpc();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

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

// 3. Add JWT Authentication & Authorization
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "JWT signing key ('Jwt:Key') is missing. Configure it via environment variable 'Jwt__Key' (e.g. $env:Jwt__Key = '...') or .NET User Secrets ('dotnet user-secrets set \"Jwt:Key\" \"...\"'). The key must be at least 32 bytes (256 bits) long.");
}
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "JWT signing key ('Jwt:Key') is too short. The key must be at least 32 bytes (256 bits) long for HMAC-SHA256.");
}
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "https://identity.falumni.local";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "falumni-services";
var clockSkewMinutes = int.TryParse(builder.Configuration["Jwt:ClockSkewMinutes"], out var minutes) ? minutes : 1;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(clockSkewMinutes)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireUserRole", policy => policy.RequireRole("USER", "ADMIN"));
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("ADMIN"));
});

// 4. Add Application Layer (Services & Repositories)
builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

// Seed default users if configured or when using InMemory / Demo environment
var seedOnStartup = !bool.TryParse(app.Configuration["Database:SeedOnStartup"], out var seed) || seed;
if (seedOnStartup)
{
    await DatabaseSeeder.SeedDefaultUsersAsync(app.Services);
}

// 5. Configure HTTP Request Pipeline
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.MapOpenApi();

app.UseRouting();
app.UseCors("AllowAll");

// Authentication MUST be before Authorization
app.UseAuthentication();
app.UseAuthorization();

// Health Check & Render Wake-up Ping Endpoints (Publicly accessible for 60s cron/bot pinger)
app.MapHealthChecks("/health");
app.MapGet("/ping", () => Results.Ok(new
{
    status = "awake",
    service = "f-alumni-user-service",
    timestampUtc = DateTime.UtcNow
})).AllowAnonymous();

app.MapGet("/api/v1/ping", () => Results.Ok(new
{
    status = "awake",
    service = "f-alumni-user-service",
    timestampUtc = DateTime.UtcNow
})).AllowAnonymous();

app.MapGet("/", () => Results.Ok(new
{
    service = "FPT Alumni Connect — User Service (Auth, Profile, Directory, gRPC)",
    status = "Running",
    endpoints = new[]
    {
        "GET  /health",
        "GET  /ping",
        "GET  /api/v1/ping",
        "GET  /openapi/v1.json",
        "POST /api/v1/auth/register",
        "POST /api/v1/auth/verify-email",
        "POST /api/v1/auth/login",
        "POST /api/v1/auth/refresh-token",
        "POST /api/v1/auth/forgot-password",
        "POST /api/v1/auth/reset-password",
        "GET  /api/v1/users",
        "GET  /api/v1/users/directory",
        "GET  /api/v1/users/me",
        "PUT  /api/v1/users/me",
        "PUT  /api/v1/users/me/avatar",
        "GET  /api/v1/users/{id}",
        "POST /api/v1/users/{id}/suspend",
        "POST /api/v1/users/{id}/unsuspend",
        "PUT  /api/v1/users/{id}/role",
        "POST /api/v1/internal/users/{id}/suspend",
        "POST /api/v1/internal/users/{id}/unsuspend",
        "gRPC UserProfileService/GetUserSummary"
    }
})).AllowAnonymous();

// Map gRPC Service
app.MapGrpcService<UserProfileGrpcService>();

// Map API Controllers
app.MapControllers();

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
