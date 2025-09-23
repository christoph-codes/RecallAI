using System.ComponentModel.DataAnnotations;

namespace RecallAI.Api.Models;

public class Collection
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid ProfileId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [MaxLength(50)]
    public string Color { get; set; } = "#3B82F6";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Profile Profile { get; set; } = null!;
    public virtual ICollection<MemoryCollection> MemoryCollections { get; set; } = new List<MemoryCollection>();
}