using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using RecallAI.Api.Data;
using RecallAI.Api.Interfaces;
using RecallAI.Api.Models.Configuration;
using RecallAI.Api.Repositories;
using RecallAI.Api.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Register NpgsqlDataSource as singleton to avoid recreating it
builder.Services.AddSingleton<NpgsqlDataSource>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var connString = ResolveConnectionString(configuration);
    
    var dsb = new NpgsqlDataSourceBuilder(connString);
    dsb.EnableDynamicJson();
    dsb.UseVector();
    return dsb.Build();
});

// Configure Entity Framework with PostgreSQL and vector extension
builder.Services.AddDbContext<MemoryDbContext>((serviceProvider, options) =>
{
    // Use the singleton NpgsqlDataSource
    var dataSource = serviceProvider.GetRequiredService<NpgsqlDataSource>();

    options.UseNpgsql(dataSource, npgsqlOptions =>
    {
        npgsqlOptions.UseVector();
        npgsqlOptions.CommandTimeout(120);
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
    });

    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }

    // Configure warnings to suppress the service provider warning
    options.ConfigureWarnings(warnings =>
    {
        warnings.Log(CoreEventId.ManyServiceProvidersCreatedWarning);
    });
});


// Configure OpenAI settings
builder.Services.Configure<OpenAIConfiguration>(
    builder.Configuration.GetSection("OpenAI"));

builder.Services.Configure<HydeConfiguration>(
    builder.Configuration.GetSection("HyDE"));

// Configure Completion settings
builder.Services.Configure<RecallAI.Api.Models.Configuration.CompletionDefaults>(
    builder.Configuration.GetSection("Completion"));

// Register repositories
builder.Services.AddScoped<IMemoryRepository, MemoryRepository>();

// Register services
builder.Services.AddHttpClient<EmbeddingService>();
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddHttpClient<OpenAIService>();
builder.Services.AddHttpClient<IHydeService, HydeService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddScoped<IOpenAIService, OpenAIService>();
builder.Services.AddScoped<ICompletionPipelineService, CompletionPipelineService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(options =>
  {
      // Prevent automatic claim type mapping (e.g., "sub" to nameidentifier)
      options.MapInboundClaims = false;
      
      // Allow HTTP metadata for local development
      options.RequireHttpsMetadata = false;

      // Get the JWT secret from configuration
      var jwtSecret = builder.Configuration["Supabase:JwtSecret"];
      if (string.IsNullOrEmpty(jwtSecret))
      {
          throw new InvalidOperationException("Supabase:JwtSecret configuration is missing or empty!");
      }
      
      Console.WriteLine($"JWT Secret loaded: {jwtSecret.Substring(0, 20)}...");

      options.TokenValidationParameters = new TokenValidationParameters
      {
          // Use symmetric key validation with Supabase JWT secret
          IssuerSigningKey = new SymmetricSecurityKey(
              Encoding.UTF8.GetBytes(jwtSecret)), // Use UTF8 encoding, not Base64
          ValidIssuer = "https://oejmcrnsmkjlugnkbxbu.supabase.co/auth/v1",
          ValidAudience = "authenticated",
          ValidateIssuer = true,
          ValidateAudience = true,
          ValidateIssuerSigningKey = true,
          ValidateLifetime = true,
          ClockSkew = TimeSpan.FromMinutes(5), // More lenient clock skew
          
          // Tell ASP.NET which claim represents the user id (we want *raw* "sub")
          NameClaimType = "sub"
      };

      options.Events = new JwtBearerEvents
      {
          OnAuthenticationFailed = ctx =>
          {
              var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                 .CreateLogger("JWT");
              logger.LogError(ctx.Exception, "JWT validation failed. Token: {Token}", 
                  ctx.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "")?.Substring(0, 20) + "...");
              return Task.CompletedTask;
          },
          OnTokenValidated = ctx =>
          {
              var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                 .CreateLogger("JWT");
              var userId = ctx.Principal?.FindFirst("sub")?.Value;
              logger.LogInformation("JWT token validated successfully for user: {UserId}", userId);
              return Task.CompletedTask;
          }
      };
  });



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
            // Remove trailing slash if present
            productionOrigin = productionOrigin.TrimEnd('/');
            allowedOrigins.Add(productionOrigin);
        }

        // Add Vercel URL - handle trailing slashes
        var vercelUrl = Environment.GetEnvironmentVariable("VERCEL_URL");
        if (!string.IsNullOrEmpty(vercelUrl))
        {
            // Remove trailing slash if present
            vercelUrl = vercelUrl.TrimEnd('/');
            
            // Add with https if not already present
            if (!vercelUrl.StartsWith("http"))
            {
                allowedOrigins.Add($"https://{vercelUrl}");
            }
            else
            {
                allowedOrigins.Add(vercelUrl);
            }
        }

        // Add your specific Vercel URL directly (backup)
        allowedOrigins.Add("https://recall-ai-topaz.vercel.app");

        // Log allowed origins for debugging
        var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("CORS");
        logger.LogInformation("CORS allowed origins: {Origins}", string.Join(", ", allowedOrigins));

        policy.WithOrigins(allowedOrigins.ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
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
        Description = @"JWT Authorization header using the Bearer scheme.
                      Enter 'Bearer' [space] and then your token in the text input below.
                      Example: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
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
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
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

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors();

// Use custom JWT validation middleware instead of built-in authentication
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Configure port for Render.com deployment
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    app.Urls.Add($"http://0.0.0.0:{port}");
}

// Ensure database is created (for development) - Don't block startup
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
        var safeConnectionInfo = TryGetSafeConnectionInfo(context);
        if (!string.IsNullOrEmpty(safeConnectionInfo))
        {
            app.Logger.LogError(ex, "Failed to connect to database or create tables (Connection: {ConnectionInfo})", safeConnectionInfo);
        }
        else
        {
            app.Logger.LogError(ex, "Failed to connect to database or create tables");
        }
    }
}

await app.RunAsync();


static string ResolveConnectionString(IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    if (!string.IsNullOrWhiteSpace(connectionString) &&
        !IsPlaceholder(connectionString))
    {
        return IsLikelyDatabaseUrl(connectionString)
            ? BuildConnectionStringFromDatabaseUrl(connectionString)
            : connectionString;
    }

    foreach (var candidate in GetConnectionStringCandidates(configuration))
    {
        if (string.IsNullOrWhiteSpace(candidate) || IsPlaceholder(candidate))
        {
            continue;
        }

        if (IsLikelyDatabaseUrl(candidate))
        {
            return BuildConnectionStringFromDatabaseUrl(candidate);
        }

        if (candidate.Contains('='))
        {
            return candidate;
        }
    }

    throw new InvalidOperationException(
        "A PostgreSQL connection string was not provided. Set 'ConnectionStrings:DefaultConnection', 'DATABASE_URL', or 'SUPABASE_CONNECTION_STRING'.");
}

static IEnumerable<string?> GetConnectionStringCandidates(IConfiguration configuration)
{
    yield return configuration["Supabase:ConnectionString"];
    yield return configuration["SUPABASE_CONNECTION_STRING"];
    yield return configuration["Supabase:DatabaseUrl"];
    yield return configuration["DATABASE_URL"];
    yield return configuration["SUPABASE_DATABASE_URL"];
    yield return Environment.GetEnvironmentVariable("SUPABASE_CONNECTION_STRING");
    yield return Environment.GetEnvironmentVariable("SUPABASE_DATABASE_URL");
    yield return Environment.GetEnvironmentVariable("DATABASE_URL");
}

static bool IsPlaceholder(string? value) =>
    !string.IsNullOrWhiteSpace(value) &&
    value.Contains("YOUR_POSTGRES_CONNECTION_STRING", StringComparison.OrdinalIgnoreCase);

static bool IsLikelyDatabaseUrl(string value) =>
    value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
    value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);

static string? TryGetSafeConnectionInfo(MemoryDbContext context)
{
    try
    {
        var raw = context.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var effective = IsLikelyDatabaseUrl(raw) ? BuildConnectionStringFromDatabaseUrl(raw) : raw;
        var builder = new NpgsqlConnectionStringBuilder(effective);

        if (!string.IsNullOrWhiteSpace(builder.Password))
        {
            builder.Password = "*****";
        }

        if (!string.IsNullOrWhiteSpace(builder.Username))
        {
            builder.Username = "*****";
        }

        return $"Host={builder.Host};Port={builder.Port};Database={builder.Database};SslMode={builder.SslMode};TrustServerCertificate={builder.TrustServerCertificate}";
    }
    catch
    {
        return null;
    }
}

static string BuildConnectionStringFromDatabaseUrl(string databaseUrl)
{
    if (string.IsNullOrWhiteSpace(databaseUrl))
    {
        throw new InvalidOperationException("DATABASE_URL is empty.");
    }

    if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri))
    {
        throw new InvalidOperationException($"DATABASE_URL '{databaseUrl}' is not a valid absolute URI.");
    }

    if (!uri.Scheme.Equals("postgres", StringComparison.OrdinalIgnoreCase) &&
        !uri.Scheme.Equals("postgresql", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("DATABASE_URL must start with 'postgres://' or 'postgresql://'.");
    }

    var userInfoParts = uri.UserInfo.Split(':', 2);
    if (userInfoParts.Length == 0 || string.IsNullOrWhiteSpace(userInfoParts[0]))
    {
        throw new InvalidOperationException("DATABASE_URL must include a username.");
    }

    var builder = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.IsDefaultPort ? 5432 : uri.Port,
        Database = uri.AbsolutePath.Trim('/'),
        Username = Uri.UnescapeDataString(userInfoParts[0]),
        SslMode = SslMode.Require,
    };

    if (userInfoParts.Length == 2)
    {
        builder.Password = Uri.UnescapeDataString(userInfoParts[1]);
    }

    if (string.IsNullOrWhiteSpace(builder.Database))
    {
        throw new InvalidOperationException("DATABASE_URL must specify a database name.");
    }

    var query = uri.Query.TrimStart('?');
    if (!string.IsNullOrWhiteSpace(query))
    {
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(kv[0]);
            var value = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : string.Empty;

            if (key.Equals("sslmode", StringComparison.OrdinalIgnoreCase))
            {
                if (Enum.TryParse<SslMode>(value, true, out var sslMode))
                {
                    builder.SslMode = sslMode;
                }

                continue;
            }

            builder[key] = value;
        }
    }

    return builder.ConnectionString;
}
