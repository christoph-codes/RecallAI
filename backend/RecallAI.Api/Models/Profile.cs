using System.ComponentModel.DataAnnotations;

namespace RecallAI.Api.Models;

public class Profile
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? FirstName { get; set; }
    
    [MaxLength(100)]
    public string? LastName { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<Memory> Memories { get; set; } = new List<Memory>();
    public virtual ICollection<Collection> Collections { get; set; } = new List<Collection>();
}