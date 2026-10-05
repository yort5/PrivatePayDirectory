using System.Text.Json.Serialization;
using PrivatePayDirectory.Core.Enums;

namespace PrivatePayDirectory.Core.Models;

public class Provider
{
    // Cosmos DB requires a lowercase "id" property
    [JsonPropertyName("id")]
    public string ProviderId { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = false;
    public Profession Profession { get; set; } = Profession.Therapist;

    /// <summary>
    /// Readable, unique URL segment for the public profile (/{profession-slug}/{Slug}),
    /// e.g. "grace-fischer-austin". Assigned on save; null until the profile is first saved.
    /// </summary>
    public string? Slug { get; set; }

    // Personal info
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string? ProfilePhotoKey { get; set; }

    // Session availability — derived:
    //   OffersVirtual  = LicensedVirtualStates.Count >= 1
    //   OffersInPerson = Offices.Count >= 1
    public List<string> LicensedVirtualStates { get; set; } = [];
    public List<OfficeLocation> Offices { get; set; } = [];

    // Taxonomy (fixed dropdowns)
    public List<string> Specialties { get; set; } = [];
    public List<string> InsuranceAccepted { get; set; } = [];
    public List<string> Languages { get; set; } = [];

    // Contact
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? WebsiteUrl { get; set; }

    public bool AcceptingNewClients { get; set; } = true;

    // Reserved for future ratings feature
    public double? AverageRating { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Derived helpers
    public bool OffersVirtual => LicensedVirtualStates.Count >= 1;
    public bool OffersInPerson => Offices.Count >= 1;
}
