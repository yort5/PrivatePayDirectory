using PrivatePayDirectory.Core.Models;

namespace PrivatePayDirectory.Core.Interfaces;

public class TherapistFilter
{
    public bool? OffersVirtual { get; set; }
    public bool? OffersInPerson { get; set; }
    public string? VirtualState { get; set; }
    public string? OfficeState { get; set; }
    public string? Specialty { get; set; }
    public string? Insurance { get; set; }
    public string? Language { get; set; }
    public bool? AcceptingNewClients { get; set; }
    public string? NameContains { get; set; }
}

public interface ITherapistRepository
{
    Task<Therapist?> GetByIdAsync(string therapistId);
    Task<Therapist?> GetByUserIdAsync(string userId);
    Task<IReadOnlyList<Therapist>> GetVisibleAsync(TherapistFilter? filter = null);
    Task<IReadOnlyList<Therapist>> GetAllAsync();  // Admin use
    Task SaveAsync(Therapist therapist);
    Task SetVisibilityAsync(string therapistId, bool isVisible);
    Task DeleteAsync(string therapistId);
}
