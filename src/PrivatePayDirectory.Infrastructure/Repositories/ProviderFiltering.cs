using PrivatePayDirectory.Core.Interfaces;
using PrivatePayDirectory.Core.Models;

namespace PrivatePayDirectory.Infrastructure.Repositories;

/// <summary>Directory filter logic shared by the Cosmos and local repositories.</summary>
public static class ProviderFiltering
{
    public static IReadOnlyList<Provider> Apply(IEnumerable<Provider> providers, ProviderFilter? filter)
    {
        var q = providers;

        if (filter != null)
        {
            if (filter.Profession.HasValue)
                q = q.Where(p => p.Profession == filter.Profession.Value);
            if (filter.OffersVirtual == true)
                q = q.Where(p => p.OffersVirtual);
            if (filter.OffersInPerson == true)
                q = q.Where(p => p.OffersInPerson);
            if (!string.IsNullOrWhiteSpace(filter.VirtualState))
                q = q.Where(p => p.LicensedVirtualStates.Contains(filter.VirtualState, StringComparer.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(filter.OfficeState))
                q = q.Where(p => p.Offices.Any(o => string.Equals(o.State, filter.OfficeState, StringComparison.OrdinalIgnoreCase)));
            if (!string.IsNullOrWhiteSpace(filter.Specialty))
                q = q.Where(p => p.Specialties.Contains(filter.Specialty, StringComparer.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(filter.Language))
                q = q.Where(p => p.Languages.Contains(filter.Language, StringComparer.OrdinalIgnoreCase));
            if (filter.AcceptingNewClients.HasValue)
                q = q.Where(p => p.AcceptingNewClients == filter.AcceptingNewClients.Value);
            if (!string.IsNullOrWhiteSpace(filter.NameContains))
                q = q.Where(p => (p.FirstName + " " + p.LastName).Contains(filter.NameContains, StringComparison.OrdinalIgnoreCase));
        }

        return q.OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ToList();
    }
}
