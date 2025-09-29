using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecallAI.Api.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace RecallAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OpenAIController : ControllerBase
{
    private readonly IOpenAIService _openAIService;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<OpenAIController> _logger;

    public OpenAIController(
        IOpenAIService openAIService,
        IEmbeddingService embeddingService,
        ILogger<OpenAIController> logger)
    {
        _openAIService = openAIService;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    [HttpPost("evaluate-memory")]
    public async Task<IActionResult> EvaluateMemory([FromBody] EvaluateMemoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.MemoryContent))
            return BadRequest(new { error = "Memory content is required" });
            
        if (string.IsNullOrWhiteSpace(request.EvaluationCriteria))
            return BadRequest(new { error = "Evaluation criteria is required" });

        try
        {
            var evaluation = await _openAIService.EvaluateMemoryAsync(request.MemoryContent, request.EvaluationCriteria);
            return Ok(new { evaluation });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate memory");
            return StatusCode(500, new { error = "Failed to evaluate memory" });
        }
    }

    [HttpPost("generate-hyde")]
    public async Task<IActionResult> GenerateHyDE([FromBody] GenerateHyDERequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return BadRequest(new { error = "Query is required" });

        try
        {
            var hypotheticalDocument = await _openAIService.GenerateHyDEAsync(request.Query);
            return Ok(new { hypotheticalDocument });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate HyDE");
            return StatusCode(500, new { error = "Failed to generate HyDE" });
        }
    }

    [HttpPost("generate-final-result")]
    public async Task<IActionResult> GenerateFinalResult([FromBody] GenerateFinalResultRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest(new { error = "Prompt is required" });
            
        if (string.IsNullOrWhiteSpace(request.Context))
            return BadRequest(new { error = "Context is required" });

        try
        {
            var result = await _openAIService.GenerateFinalResultAsync(request.Prompt, request.Context);
            return Ok(new { result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate final result");
            return StatusCode(500, new { error = "Failed to generate final result" });
        }
    }

    [HttpPost("generate-embedding")]
    public async Task<IActionResult> GenerateEmbedding([FromBody] GenerateEmbeddingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest(new { error = "Text is required" });

        try
        {
            var embedding = await _embeddingService.GenerateEmbeddingAsync(request.Text);
            return Ok(new { embedding, dimensions = embedding.Length });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embedding");
            return StatusCode(500, new { error = "Failed to generate embedding" });
        }
    }
}

public record EvaluateMemoryRequest(
    [Required] string MemoryContent, 
    [Required] string EvaluationCriteria);

public record GenerateHyDERequest(
    [Required] string Query);

public record GenerateFinalResultRequest(
    [Required] string Prompt, 
    [Required] string Context);

public record GenerateEmbeddingRequest(
    [Required] string Text);
