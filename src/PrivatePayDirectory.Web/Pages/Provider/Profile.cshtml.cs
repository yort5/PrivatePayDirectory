using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrivatePayDirectory.Core.Interfaces;
using ProviderModel = PrivatePayDirectory.Core.Models.Provider;

namespace PrivatePayDirectory.Web.Pages.Provider;

public class ProfileModel(IProviderRepository providerRepo, IPhotoService photoService) : PageModel
{
    public ProviderModel Provider { get; private set; } = null!;
    public string? PhotoUrl { get; private set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var provider = await providerRepo.GetByIdAsync(id);
        if (provider == null)
            return NotFound();

        // Non-admins can only see visible profiles
        if (!provider.IsVisible && !User.IsInRole("Administrator"))
            return NotFound();

        Provider = provider;
        PhotoUrl = provider.ProfilePhotoKey != null
            ? photoService.GetPhotoUrl(provider.ProfilePhotoKey)
            : null;

        return Page();
    }
}
