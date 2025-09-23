using System.ComponentModel.DataAnnotations;
using Pgvector;

namespace RecallAI.Api.Models;

public class MemoryEmbedding
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid MemoryId { get; set; }
    
    [Required]
    public Vector Embedding { get; set; } = null!;
    
    [MaxLength(50)]
    public string Model { get; set; } = "text-embedding-3-small";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Memory Memory { get; set; } = null!;
}