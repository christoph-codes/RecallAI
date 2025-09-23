namespace RecallAI.Api.Models.Auth;

public class UserInfo
{
    public required string Id { get; set; }
    public required string Email { get; set; }
    public string? FullName { get; set; }
    public DateTime CreatedAt { get; set; }
}