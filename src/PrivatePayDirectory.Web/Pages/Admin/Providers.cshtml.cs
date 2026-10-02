using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrivatePayDirectory.Core.Enums;
using PrivatePayDirectory.Core.Interfaces;
using PrivatePayDirectory.Core.Models;
using PrivatePayDirectory.Web;
using System.Security.Claims;
using ProviderModel = PrivatePayDirectory.Core.Models.Provider;

namespace PrivatePayDirectory.Web.Pages.Admin;

[Authorize(Policy = Policies.RequireAdmin)]
public class ProvidersModel(
    IProviderRepository providerRepo,
    IUserRepository userRepo,
    IPhotoService photoService,
    INotificationService notifications) : PageModel
{
    public IReadOnlyList<ProviderModel> Pending { get; private set; } = [];
    public IReadOnlyList<ProviderModel> Approved { get; private set; } = [];

    public string GetPhotoUrl(string key) => photoService.GetPhotoUrl(key);
    public string GetProfessionDisplay(Profession p) => Taxonomy.ProfessionDisplay[p];

    public async Task OnGetAsync()
    {
        var scope = await GetAdminScopeAsync();

        var all = (await providerRepo.GetAllAsync())
            .OrderBy(p => p.CreatedAt)
            .ToList();

        if (scope is { Count: > 0 })
            all = all.Where(p => scope.Contains(p.Profession)).ToList();

        Pending = all.Where(p => !p.IsVisible).ToList();
        Approved = all.Where(p => p.IsVisible).OrderBy(p => p.LastName).ToList();
    }

    public async Task<IActionResult> OnPostAsync(string providerId, bool isVisible)
    {
        var provider = await providerRepo.GetByIdAsync(providerId);
        if (provider == null) return NotFound();

        // Defense in depth: verify this admin can manage the target provider's profession
        var scope = await GetAdminScopeAsync();
        if (scope is { Count: > 0 } && !scope.Contains(provider.Profession))
            return Forbid();

        await providerRepo.SetVisibilityAsync(providerId, isVisible);

        if (isVisible)
        {
            var user = await userRepo.GetByIdAsync(provider.UserId);
            if (user != null && user.Role == UserRole.Standard)
            {
                user.Role = UserRole.Provider;
                await userRepo.SaveAsync(user);
            }
            await notifications.NotifyProfileApprovedAsync(provider);
        }
        else
        {
            var user = await userRepo.GetByIdAsync(provider.UserId);
            if (user != null && user.Role == UserRole.Provider)
            {
                user.Role = UserRole.Standard;
                await userRepo.SaveAsync(user);
            }
        }

        TempData["Success"] = isVisible
            ? $"{provider.FirstName} {provider.LastName}'s profile is now live in the directory."
            : $"{provider.FirstName} {provider.LastName}'s profile has been hidden.";

        return RedirectToPage();
    }

    /// <summary>Returns the current admin's managed professions, or null if unscoped (can manage all).</summary>
    private async Task<List<Profession>?> GetAdminScopeAsync()
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (adminUserId == null) return null;
        var adminUser = await userRepo.GetByIdAsync(adminUserId);
        return adminUser?.ManagedProfessions is { Count: > 0 } scope ? scope : null;
    }
}
