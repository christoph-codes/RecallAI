using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecallAI.Api.Models;

public class Collection
{
    [Key]
    public Guid Id { get; set; }
    
    public Guid? UserId { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public string Color { get; set; } = "#6366f1";
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    
    // Navigation properties
    [ForeignKey("UserId")]
    public virtual Profile? Profile { get; set; }
    public virtual ICollection<Memory> Memories { get; set; } = new List<Memory>();
}