using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
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

// Configure Entity Framework with PostgreSQL and vector extension
builder.Services.AddDbContext<MemoryDbContext>(options =>
{
    var connString = builder.Configuration.GetConnectionString("DefaultConnection");

    // Build an NpgsqlDataSource and enable dynamic JSON
    var dsb = new NpgsqlDataSourceBuilder(connString);
    dsb.EnableDynamicJson();
    var dataSource = dsb.Build();

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
});


// Configure OpenAI settings
builder.Services.Configure<OpenAIConfiguration>(
    builder.Configuration.GetSection("OpenAI"));

// Configure Completion settings
builder.Services.Configure<RecallAI.Api.Models.Configuration.CompletionDefaults>(
    builder.Configuration.GetSection("Completion"));

// Register repositories
builder.Services.AddScoped<IMemoryRepository, MemoryRepository>();

// Register services
builder.Services.AddHttpClient<EmbeddingService>();
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddHttpClient<OpenAIService>();
builder.Services.AddScoped<IOpenAIService, OpenAIService>();
builder.Services.AddScoped<ICompletionPipelineService, CompletionPipelineService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(options =>
  {
      // Prevent automatic claim type mapping (e.g., "sub" → nameidentifier)
      options.MapInboundClaims = false;

      options.TokenValidationParameters = new TokenValidationParameters
      {
          IssuerSigningKey = new SymmetricSecurityKey(
          Encoding.UTF8.GetBytes(builder.Configuration["Supabase:JwtSecret"]!)),
          ValidIssuer = "https://oejmcrnsmkjlugnkbxbu.supabase.co/auth/v1",
          ValidAudience = "authenticated",
          ValidateIssuer = true,
          ValidateAudience = true,
          ValidateIssuerSigningKey = true,
          ValidateLifetime = true,
          ClockSkew = TimeSpan.FromMinutes(2),

          // Tell ASP.NET which claim represents the user id (we want *raw* "sub")
          NameClaimType = "sub"
      };

      options.Events = new JwtBearerEvents
      {
          OnAuthenticationFailed = ctx =>
          {
              ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                 .CreateLogger("JWT").LogError(ctx.Exception, "JWT validation failed");
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

app.UseHttpsRedirection();

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
