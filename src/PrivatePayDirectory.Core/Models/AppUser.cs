using System.Text.Json.Serialization;
using PrivatePayDirectory.Core.Enums;

namespace PrivatePayDirectory.Core.Models;

public class AppUser
{
    [JsonPropertyName("id")]
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public UserRole Role { get; set; } = UserRole.Standard;
    public string? TherapistId { get; set; }
    public string? PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
