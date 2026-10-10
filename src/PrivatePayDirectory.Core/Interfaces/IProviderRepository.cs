using PrivatePayDirectory.Core.Enums;
using PrivatePayDirectory.Core.Models;

namespace PrivatePayDirectory.Core.Interfaces;

public class ProviderFilter
{
    public Profession? Profession { get; set; }
    public bool? OffersVirtual { get; set; }
    public bool? OffersInPerson { get; set; }
    public string? VirtualState { get; set; }
    public string? OfficeState { get; set; }
    public string? Specialty { get; set; }
    public string? Language { get; set; }
    public bool? AcceptingNewClients { get; set; }
    public string? NameContains { get; set; }
}

public interface IProviderRepository
{
    Task<Provider?> GetByIdAsync(string providerId);
    Task<Provider?> GetByUserIdAsync(string userId);
    Task<IReadOnlyList<Provider>> GetVisibleAsync(ProviderFilter? filter = null);
    Task<IReadOnlyList<Provider>> GetAllAsync();  // Admin use
    Task SaveAsync(Provider provider);
    Task SetVisibilityAsync(string providerId, bool isVisible);
    Task DeleteAsync(string providerId);
}
