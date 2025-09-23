using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecallAI.Api.Data;

namespace RecallAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly MemoryDbContext _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(MemoryDbContext context, ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            // Test database connection
            await _context.Database.CanConnectAsync();
            
            _logger.LogInformation("Health check passed - database connection successful");
            
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                database = "connected",
                version = "1.0.0"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed - database connection error");
            
            return StatusCode(503, new
            {
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                database = "disconnected",
                error = ex.Message
            });
        }
    }
}