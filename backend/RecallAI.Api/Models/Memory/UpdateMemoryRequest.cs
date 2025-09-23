using System.ComponentModel.DataAnnotations;

namespace RecallAI.Api.Models.Dto;

public class UpdateMemoryRequest
{
    [MaxLength(200)]
    public string? Title { get; set; }
    
    [MaxLength(10000)]
    public string? Content { get; set; }
    
    [RegularExpression("^(text|document|note|conversation)$", ErrorMessage = "ContentType must be one of: text, document, note, conversation")]
    public string? ContentType { get; set; }
    
    public Dictionary<string, object>? Metadata { get; set; }
}