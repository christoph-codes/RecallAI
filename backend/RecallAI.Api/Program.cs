using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RecallAI.Api.Data;
using RecallAI.Api.Interfaces;
using RecallAI.Api.Models.Configuration;
using RecallAI.Api.Repositories;
using RecallAI.Api.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Database Context with Npgsql and pgvector
var envConnectionString = Environment.GetEnvironmentVariable("SUPABASE_CONNECTION_STRING");
var configConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var connectionString = envConnectionString ?? configConnectionString;


if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Database connection string is not configured. Please set SUPABASE_CONNECTION_STRING environment variable or DefaultConnection in appsettings.json");
}

builder.Services.AddDbContext<MemoryDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.UseVector();
        npgsqlOptions.CommandTimeout(120); // Increase timeout to 2 minutes
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
    });
    
    // Configure query behavior
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    
    // Enable sensitive data logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Configure OpenAI settings
builder.Services.Configure<OpenAIConfiguration>(
    builder.Configuration.GetSection("OpenAI"));

// Register repositories
builder.Services.AddScoped<IMemoryRepository, MemoryRepository>();

// Register services
builder.Services.AddHttpClient<EmbeddingService>();
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddHttpClient<OpenAIService>();
builder.Services.AddScoped<IOpenAIService, OpenAIService>();

// JWT Authentication for Supabase tokens
var jwtSecret = Environment.GetEnvironmentVariable("SUPABASE_JWT_SECRET")
    ?? builder.Configuration["Supabase:JwtSecret"];

if (string.IsNullOrEmpty(jwtSecret))
{
    throw new InvalidOperationException("JWT secret is not configured. Please set SUPABASE_JWT_SECRET environment variable or Supabase:JwtSecret in appsettings.json");
}

// Decode base64 JWT secret if needed
byte[] key;
try
{
    key = Convert.FromBase64String(jwtSecret);
}
catch
{
    // If it's not base64, treat as plain text
    key = Encoding.UTF8.GetBytes(jwtSecret);
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false, // Supabase doesn't always set issuer consistently
            ValidateAudience = false, // Supabase doesn't always set audience consistently
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RequireExpirationTime = true
        };

        // Configure JWT events for better error handling
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers["Token-Expired"] = "true";
                }
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                var result = System.Text.Json.JsonSerializer.Serialize(new
                {
                    error = "Authorization required",
                    message = "A valid JWT token is required to access this resource",
                    statusCode = 401,
                    timestamp = DateTime.UtcNow
                });
                return context.Response.WriteAsync(result);
            }
        };
    });

builder.Services.AddAuthorization();

// CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = new List<string>
        {
            "http://localhost:3000",
            "https://localhost:3000"
        };

        // Add production frontend URLs from environment variables or configuration
        var productionOrigin = Environment.GetEnvironmentVariable("FRONTEND_URL");
        if (!string.IsNullOrEmpty(productionOrigin))
        {
            allowedOrigins.Add(productionOrigin);
        }

        // Add common deployment platforms
        var vercelUrl = Environment.GetEnvironmentVariable("VERCEL_URL");
        if (!string.IsNullOrEmpty(vercelUrl))
        {
            allowedOrigins.Add($"https://{vercelUrl}");
        }

        policy.WithOrigins(allowedOrigins.ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "RecallAI Memory Layer API", 
        Version = "v1",
        Description = "AI Memory Layer API for storing and retrieving memories with vector embeddings"
    });
    
    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Basic logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "RecallAI Memory Layer API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseCors();

// Add custom JWT validation middleware before authentication
app.UseMiddleware<RecallAI.Api.Middleware.JwtValidationMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Configure port for Render.com deployment
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    app.Urls.Add($"http://0.0.0.0:{port}");
}

// Ensure database is created (for development)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<MemoryDbContext>();
    try
    {
        await context.Database.EnsureCreatedAsync();
        app.Logger.LogInformation("Database connection verified and tables created if needed");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to connect to database or create tables");
    }
}

await app.RunAsync();
