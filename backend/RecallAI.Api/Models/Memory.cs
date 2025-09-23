using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecallAI.Api.Models;

public class Memory
{
    [Key]
    public Guid Id { get; set; }
    
    public Guid? UserId { get; set; }
    
    public string? Title { get; set; }
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public string ContentType { get; set; } = "text";
    
    [Column(TypeName = "jsonb")]
    public Dictionary<string, object>? Metadata { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    
    // Navigation properties
    [ForeignKey("UserId")]
    public virtual Profile? Profile { get; set; }
    public virtual ICollection<MemoryEmbedding> MemoryEmbeddings { get; set; } = new List<MemoryEmbedding>();
    public virtual ICollection<Collection> Collections { get; set; } = new List<Collection>();
}