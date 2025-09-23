using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

namespace RecallAI.Api.Models;

public class MemoryEmbedding
{
    [Key]
    public Guid Id { get; set; }
    
    public Guid? MemoryId { get; set; }
    
    public Vector? Embedding { get; set; }
    
    public string ModelName { get; set; } = "text-embedding-3-small";
    
    public DateTimeOffset CreatedAt { get; set; }
    
    // Navigation properties
    [ForeignKey("MemoryId")]
    public virtual Memory? Memory { get; set; }
}