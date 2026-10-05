using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrivatePayDirectory.Core.Interfaces;
using PrivatePayDirectory.Core.Models;
using ProviderModel = PrivatePayDirectory.Core.Models.Provider;

namespace PrivatePayDirectory.Web.Pages.Provider;

/// <summary>
/// Public profile. Canonical URL is /{profession}/{providerSlug}; the older /Provider/Profile/{id}
/// route redirects there once the profile has a slug.
/// </summary>
public class ProfileModel(IProviderRepository providerRepo, IPhotoService photoService) : PageModel
{
    public ProviderModel Provider { get; private set; } = null!;
    public string? PhotoUrl { get; private set; }
    public bool IsDemo => Provider.UserId == DemoData.DemoUserId;

    public async Task<IActionResult> OnGetAsync(string? id, string? profession, string? providerSlug)
    {
        var provider = providerSlug != null
            ? await providerRepo.GetBySlugAsync(providerSlug)
            : id != null ? await providerRepo.GetByIdAsync(id) : null;
        if (provider == null)
            return NotFound();

        // Non-admins can only see visible profiles
        if (!provider.IsVisible && !User.IsInRole("Administrator"))
            return NotFound();

        // One URL per profile: send the id route, or a wrong profession segment, to the canonical path
        var canonical = ProviderUrls.ProfilePath(provider);
        if (!string.Equals(Request.Path.Value, canonical, StringComparison.OrdinalIgnoreCase))
            return RedirectPermanent(canonical);

        Provider = provider;
        PhotoUrl = provider.ProfilePhotoKey != null
            ? photoService.GetPhotoUrl(provider.ProfilePhotoKey)
            : null;

        return Page();
    }
}
