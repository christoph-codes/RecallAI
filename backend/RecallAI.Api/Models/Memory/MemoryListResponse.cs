namespace RecallAI.Api.Models.Dto;

public class MemoryListResponse
{
    public List<MemoryResponse> Memories { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}