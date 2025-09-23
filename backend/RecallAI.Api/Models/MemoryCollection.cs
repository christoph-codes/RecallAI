using System.ComponentModel.DataAnnotations;

namespace RecallAI.Api.Models;

public class MemoryCollection
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid MemoryId { get; set; }
    
    [Required]
    public Guid CollectionId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Memory Memory { get; set; } = null!;
    public virtual Collection Collection { get; set; } = null!;
}