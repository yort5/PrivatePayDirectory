using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrivatePayDirectory.Core.Enums;
using PrivatePayDirectory.Core.Interfaces;
using PrivatePayDirectory.Core.Models;
using System.Security.Claims;

namespace PrivatePayDirectory.Web.Pages.Provider;

public class EditModel(
    IProviderRepository providerRepo,
    IPhotoService photoService) : PageModel
{
    [BindProperty]
    public ProviderInputModel Input { get; set; } = new();

    public string? ProviderId { get; private set; }
    public string? CurrentPhotoUrl { get; private set; }
    public bool IsVisible { get; private set; }
    public Profession Profession { get; private set; }

    public IReadOnlyList<string> AllStates => Taxonomy.UnitedStates;
    public IReadOnlyList<string> AllSpecialties => Taxonomy.SpecialtiesByProfession[Profession];
    public IReadOnlyList<string> AllInsurance => Taxonomy.InsurancePlans;
    public IReadOnlyList<string> AllLanguages => Taxonomy.Languages;

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var provider = await providerRepo.GetByUserIdAsync(userId);
        if (provider == null)
            return RedirectToPage("/Provider/Register");

        ProviderId = provider.ProviderId;
        IsVisible = provider.IsVisible;
        Profession = provider.Profession;
        CurrentPhotoUrl = provider.ProfilePhotoKey != null
            ? photoService.GetPhotoUrl(provider.ProfilePhotoKey)
            : null;

        Input = ProviderInputModel.FromProvider(provider);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var provider = await providerRepo.GetByUserIdAsync(userId);
        if (provider == null) return NotFound();

        Input.ApplyTo(provider);
        await providerRepo.SaveAsync(provider);

        TempData["Success"] = "Profile saved successfully.";
        return RedirectToPage();
    }

    // AJAX handler — returns JSON so the page stays in place
    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
            return new JsonResult(new { success = false, error = "Please fill in all required fields." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var provider = await providerRepo.GetByUserIdAsync(userId);
        if (provider == null)
            return new JsonResult(new { success = false, error = "Profile not found." });

        Input.ApplyTo(provider);
        provider.UpdatedAt = DateTime.UtcNow;
        await providerRepo.SaveAsync(provider);

        return new JsonResult(new { success = true });
    }
}

public class ProviderInputModel
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string? ProfilePhotoKey { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? WebsiteUrl { get; set; }
    public bool AcceptingNewClients { get; set; } = true;
    public List<string> LicensedVirtualStates { get; set; } = [];
    public List<OfficeLocation> Offices { get; set; } = [];
    public List<string> Specialties { get; set; } = [];
    public List<string> InsuranceAccepted { get; set; } = [];
    public List<string> Languages { get; set; } = [];

    public static ProviderInputModel FromProvider(Core.Models.Provider p) => new()
    {
        FirstName = p.FirstName,
        LastName = p.LastName,
        Title = p.Title,
        Bio = p.Bio,
        ProfilePhotoKey = p.ProfilePhotoKey,
        Phone = p.Phone,
        Email = p.Email,
        // Strip scheme so the input only shows the host/path part
        WebsiteUrl = p.WebsiteUrl != null
            ? System.Text.RegularExpressions.Regex.Replace(p.WebsiteUrl, @"^https?://", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            : null,
        AcceptingNewClients = p.AcceptingNewClients,
        LicensedVirtualStates = [.. p.LicensedVirtualStates],
        Offices = [.. p.Offices],
        Specialties = [.. p.Specialties],
        InsuranceAccepted = [.. p.InsuranceAccepted],
        Languages = [.. p.Languages],
    };

    public void ApplyTo(Core.Models.Provider p)
    {
        p.FirstName = FirstName;
        p.LastName = LastName;
        p.Title = Title;
        p.Bio = Bio;
        p.Phone = Phone;
        p.Email = Email;

        // Prepend https:// to the host-only value from the input
        if (!string.IsNullOrWhiteSpace(WebsiteUrl))
            p.WebsiteUrl = "https://" + WebsiteUrl.Trim().TrimStart('/');
        else
            p.WebsiteUrl = null;

        p.AcceptingNewClients = AcceptingNewClients;
        p.LicensedVirtualStates = [.. LicensedVirtualStates];
        p.Offices = [.. Offices];
        p.Specialties = [.. Specialties];
        p.InsuranceAccepted = [.. InsuranceAccepted];
        p.Languages = [.. Languages];
        if (!string.IsNullOrEmpty(ProfilePhotoKey))
            p.ProfilePhotoKey = ProfilePhotoKey;
    }
}
