using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Template.Api.Middlewares;
using Template.Services.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Logging & Configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// 2. Add Framework Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

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

// 5. Configure HTTP Request Pipeline
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRouting();

// Authentication MUST be before Authorization
app.UseAuthentication();
app.UseAuthorization();

// Health Check Endpoint (Publicly accessible for monitoring/orchestration)
app.MapHealthChecks("/health");

// Map API Controllers
app.MapControllers();

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
