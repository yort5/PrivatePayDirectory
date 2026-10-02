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

    public string? ProviderId { get; set; }

    /// <summary>
    /// Professions this admin is allowed to manage. Null/empty = unscoped (can manage all professions).
    /// Only meaningful when Role == Administrator.
    /// </summary>
    public List<Profession>? ManagedProfessions { get; set; }

    public string? PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
