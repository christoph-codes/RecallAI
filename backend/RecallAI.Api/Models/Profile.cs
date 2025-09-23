using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecallAI.Api.Models;

public class Profile
{
    [Key]
    public Guid Id { get; set; }
    
    public string? Email { get; set; }
    
    public string? FullName { get; set; }
    
    public string? AvatarUrl { get; set; }
    
    [Column(TypeName = "jsonb")]
    public Dictionary<string, object>? Preferences { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<Memory> Memories { get; set; } = new List<Memory>();
    public virtual ICollection<Collection> Collections { get; set; } = new List<Collection>();
}