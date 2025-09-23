using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecallAI.Api.Models;

public class MemoryCollection
{
    [Required]
    public Guid MemoryId { get; set; }
    
    [Required]
    public Guid CollectionId { get; set; }
    
    public DateTimeOffset AddedAt { get; set; }
    
    // Navigation properties
    [ForeignKey("MemoryId")]
    public virtual Memory Memory { get; set; } = null!;
    
    [ForeignKey("CollectionId")]
    public virtual Collection Collection { get; set; } = null!;
}