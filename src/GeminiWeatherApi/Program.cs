using System.Text;
using GeminiWeatherApi.Data;
using GeminiWeatherApi.Data.Entities;
using GeminiWeatherApi.Infrastructure;
using GeminiWeatherApi.Options;
using GeminiWeatherApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using Microsoft.IdentityModel.Tokens;

// dotnet run --project can use the project folder as the working directory;
// TraversePath also finds a developer's .env in the repository root.
DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
var isDevelopment = builder.Environment.IsDevelopment();

var databaseConnection =
    builder.Configuration["DATABASE_CONNECTION"]
    ?? builder.Configuration.GetConnectionString("Database")
    ?? (isDevelopment
        ? "Host=localhost;Port=5432;Database=gemini_weather;Username=gemini_weather;Password=local-dev-only"
        : throw new InvalidOperationException(
            "DATABASE_CONNECTION must be configured outside the Development environment."));

var redisConnection =
    builder.Configuration["REDIS_CONNECTION"]
    ?? builder.Configuration["ConnectionStrings:Redis"]
    ?? (isDevelopment
        ? "localhost:6379"
        : throw new InvalidOperationException(
            "REDIS_CONNECTION must be configured outside the Development environment."));

var jwtSigningKey =
    builder.Configuration["JWT_SIGNING_KEY"]
    ?? builder.Configuration["Jwt:SigningKey"]
    ?? (isDevelopment
        ? "local-development-signing-key-change-me-please-32-chars"
        : throw new InvalidOperationException(
            "JWT_SIGNING_KEY must be configured outside the Development environment."));

var jwtIssuer =
    builder.Configuration["JWT_ISSUER"]
    ?? builder.Configuration["Jwt:Issuer"]
    ?? "GeminiWeatherApi";

var jwtAudience =
    builder.Configuration["JWT_AUDIENCE"]
    ?? builder.Configuration["Jwt:Audience"]
    ?? "GeminiWeatherApi.Client";

var accessTokenMinutes = int.TryParse(
    builder.Configuration["JWT_ACCESS_TOKEN_MINUTES"],
    out var configuredMinutes)
    ? configuredMinutes
    : 60;

builder.Services.AddOptions<GeminiOptions>()
    .Configure(options =>
    {
        options.ApiKey = builder.Configuration["GEMINI_API_KEY"]
            ?? builder.Configuration["Gemini:ApiKey"]
            ?? string.Empty;
        options.Model = builder.Configuration["GEMINI_MODEL"]
            ?? builder.Configuration["Gemini:Model"]
            ?? "gemini-3.7-flash";
    })
    .Validate(options => !string.IsNullOrWhiteSpace(options.ApiKey),
        "GEMINI_API_KEY must be configured before starting the API.")
    .ValidateOnStart();

builder.Services.AddOptions<JwtOptions>()
    .Configure(options =>
    {
        options.SigningKey = jwtSigningKey;
        options.Issuer = jwtIssuer;
        options.Audience = jwtAudience;
        options.AccessTokenMinutes = accessTokenMinutes;
    })
    .Validate(options => options.SigningKey.Length >= 32,
        "JWT_SIGNING_KEY must contain at least 32 characters.")
    .Validate(options => options.AccessTokenMinutes is >= 5 and <= 1440,
        "JWT_ACCESS_TOKEN_MINUTES must be between 5 and 1440.")
    .ValidateOnStart();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(databaseConnection, npgsql =>
        npgsql.EnableRetryOnFailure()));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
    options.InstanceName = "gemini-weather:";
});

builder.Services.AddHttpClient<IGeminiClient, GeminiClient>(client =>
{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
    client.Timeout = TimeSpan.FromSeconds(45);
});

builder.Services.AddHttpClient<IOpenMeteoClient, OpenMeteoClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
});

builder.Services.AddSingleton<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IWeatherBriefService, WeatherBriefService>();

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Gemini Weather API",
        Version = "v1",
        Description = "A .NET API that turns live weather data into structured Gemini answers."
    });
    options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter your JWT token. Example: eyJhbGciOi..."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("bearer", document)] = []
    });
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("postgres");

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("ai", limiter =>
    {
        limiter.PermitLimit = 20;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseExceptionHandler();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Gemini Weather API v1");
    options.RoutePrefix = "swagger";
    options.DocumentTitle = "Gemini Weather API - Swagger";
});
app.MapControllers();

app.MapGet("/health/live", () => Results.Ok(new
{
    status = "ok",
    service = "gemini-weather-api",
    timestampUtc = DateTimeOffset.UtcNow
}));

app.MapHealthChecks("/health/ready");

await ApplyDatabaseAsync(app.Services);

await app.RunAsync();

static async Task ApplyDatabaseAsync(IServiceProvider services)
{
    await using var scope = services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await database.Database.MigrateAsync();
}

public partial class Program;
