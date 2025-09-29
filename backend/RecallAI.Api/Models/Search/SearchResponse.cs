namespace RecallAI.Api.Models.Search;

public class SearchResponse
{
    public List<SearchResultItem> Results { get; set; } = new List<SearchResultItem>();
    
    public string Query { get; set; } = string.Empty;
    
    public int ResultCount { get; set; }
    
    public int ExecutionTimeMs { get; set; }
    
    public bool HydeUsed { get; set; }
    
    public string? HypotheticalDocument { get; set; }
}
