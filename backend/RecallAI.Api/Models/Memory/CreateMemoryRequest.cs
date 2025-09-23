using System.ComponentModel.DataAnnotations;

namespace RecallAI.Api.Models.Dto;

public class CreateMemoryRequest
{
    [MaxLength(200)]
    public string? Title { get; set; }
    
    [Required]
    [MaxLength(10000)]
    public string Content { get; set; } = string.Empty;
    
    [RegularExpression("^(text|document|note|conversation)$", ErrorMessage = "ContentType must be one of: text, document, note, conversation")]
    public string? ContentType { get; set; } = "text";
    
    public Dictionary<string, object>? Metadata { get; set; }
}