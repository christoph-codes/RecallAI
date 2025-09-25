using System.ComponentModel.DataAnnotations;

namespace RecallAI.Api.Models.Search;

public class SearchResultItem
{
    public Guid Id { get; set; }
    
    public string? Title { get; set; }
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public string ContentType { get; set; } = "text";
    
    public double SimilarityScore { get; set; }
    
    public double CombinedScore { get; set; }
    
    public string SearchMethod { get; set; } = "query";
    
    public DateTimeOffset CreatedAt { get; set; }
    
    public Dictionary<string, object>? Metadata { get; set; }
}
