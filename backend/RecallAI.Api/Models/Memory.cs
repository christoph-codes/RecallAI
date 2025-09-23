using System.ComponentModel.DataAnnotations;

namespace RecallAI.Api.Models;

public class Memory
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid ProfileId { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? Type { get; set; }
    
    [MaxLength(1000)]
    public string? Metadata { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Profile Profile { get; set; } = null!;
    public virtual MemoryEmbedding? Embedding { get; set; }
    public virtual ICollection<MemoryCollection> MemoryCollections { get; set; } = new List<MemoryCollection>();
}