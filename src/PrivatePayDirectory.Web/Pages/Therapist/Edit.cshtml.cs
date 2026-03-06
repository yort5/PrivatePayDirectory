using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrivatePayDirectory.Core.Interfaces;
using PrivatePayDirectory.Core.Models;
using System.Security.Claims;

namespace PrivatePayDirectory.Web.Pages.Therapist;

public class EditModel(
    ITherapistRepository therapistRepo,
    IPhotoService photoService) : PageModel
{
    [BindProperty]
    public TherapistInputModel Input { get; set; } = new();

    public string? TherapistId { get; private set; }
    public string? CurrentPhotoUrl { get; private set; }
    public bool IsVisible { get; private set; }

    public IReadOnlyList<string> AllStates => Taxonomy.UnitedStates;
    public IReadOnlyList<string> AllSpecialties => Taxonomy.Specialties;
    public IReadOnlyList<string> AllInsurance => Taxonomy.InsurancePlans;
    public IReadOnlyList<string> AllLanguages => Taxonomy.Languages;

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var therapist = await therapistRepo.GetByUserIdAsync(userId);
        if (therapist == null)
            return RedirectToPage("/Therapist/Register");

        TherapistId = therapist.TherapistId;
        IsVisible = therapist.IsVisible;
        CurrentPhotoUrl = therapist.ProfilePhotoKey != null
            ? photoService.GetPhotoUrl(therapist.ProfilePhotoKey)
            : null;

        Input = TherapistInputModel.FromTherapist(therapist);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var therapist = await therapistRepo.GetByUserIdAsync(userId);
        if (therapist == null) return NotFound();

        Input.ApplyTo(therapist);
        await therapistRepo.SaveAsync(therapist);

        TempData["Success"] = "Profile saved successfully.";
        return RedirectToPage();
    }
}

public class TherapistInputModel
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

    public static TherapistInputModel FromTherapist(Core.Models.Therapist t) => new()
    {
        FirstName = t.FirstName,
        LastName = t.LastName,
        Title = t.Title,
        Bio = t.Bio,
        ProfilePhotoKey = t.ProfilePhotoKey,
        Phone = t.Phone,
        Email = t.Email,
        WebsiteUrl = t.WebsiteUrl,
        AcceptingNewClients = t.AcceptingNewClients,
        LicensedVirtualStates = [.. t.LicensedVirtualStates],
        Offices = [.. t.Offices],
        Specialties = [.. t.Specialties],
        InsuranceAccepted = [.. t.InsuranceAccepted],
        Languages = [.. t.Languages],
    };

    public void ApplyTo(Core.Models.Therapist t)
    {
        t.FirstName = FirstName;
        t.LastName = LastName;
        t.Title = Title;
        t.Bio = Bio;
        t.Phone = Phone;
        t.Email = Email;
        t.WebsiteUrl = WebsiteUrl;
        t.AcceptingNewClients = AcceptingNewClients;
        t.LicensedVirtualStates = [.. LicensedVirtualStates];
        t.Offices = [.. Offices];
        t.Specialties = [.. Specialties];
        t.InsuranceAccepted = [.. InsuranceAccepted];
        t.Languages = [.. Languages];
        if (!string.IsNullOrEmpty(ProfilePhotoKey))
            t.ProfilePhotoKey = ProfilePhotoKey;
    }
}
