using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using PrivatePayDirectory.Core.Interfaces;
using PrivatePayDirectory.Core.Models;
using ProviderModel = PrivatePayDirectory.Core.Models.Provider;

namespace PrivatePayDirectory.Web.Pages;

/// <summary>One directory page per profession, at /{slug} (e.g. /chiropractors).</summary>
public class DirectoryModel(IProviderRepository providerRepo, IPhotoService photoService) : PageModel
{
    public ProfessionInfo Profession { get; private set; } = null!;
    public IReadOnlyList<ProviderModel> Providers { get; private set; } = [];
    public ProviderFilter Filter { get; private set; } = new();
    public string? SelectedSessionType { get; private set; }
    public string? SelectedState { get; private set; }

    public IReadOnlyList<(string Country, IReadOnlyList<string> Codes)> Regions => Taxonomy.RegionsByCountry;
    public IReadOnlyList<string> Specialties => Taxonomy.SpecialtiesByProfession[Profession.Profession];
    public IReadOnlyList<string> Languages => Taxonomy.Languages;

    /// <summary>The other professions, for the cross-promotion section.</summary>
    public IEnumerable<ProfessionInfo> OtherProfessions =>
        Taxonomy.Professions.Where(p => p.Profession != Profession.Profession);

    public List<SelectListItem> SessionTypeOptions =>
    [
        new SelectListItem("Virtual", "virtual", SelectedSessionType == "virtual"),
        new SelectListItem("In-Person", "inperson", SelectedSessionType == "inperson"),
        new SelectListItem("Both", "both", SelectedSessionType == "both"),
    ];

    public async Task<IActionResult> OnGetAsync(
        string slug,
        string? sessionType,
        string? state,
        string? specialty,
        string? language,
        bool? acceptingClients,
        string? name)
    {
        // The route constraint already guarantees a known slug
        Profession = Taxonomy.FindBySlug(slug)!;
        SelectedSessionType = sessionType;
        state = string.IsNullOrWhiteSpace(state) ? null : state.Trim().ToUpperInvariant();
        SelectedState = state;

        Filter = new ProviderFilter
        {
            Profession = Profession.Profession,
            OffersVirtual = sessionType is "virtual" or "both" ? true : null,
            OffersInPerson = sessionType is "inperson" or "both" ? true : null,
            // A state/province means: licensed there (virtual), has an office there (in-person),
            // both for "both", and either one when no session mode is chosen
            VirtualState = sessionType is "virtual" or "both" ? state : null,
            OfficeState = sessionType is "inperson" or "both" ? state : null,
            AnyState = sessionType is "virtual" or "inperson" or "both" ? null : state,
            Specialty = specialty,
            Language = language,
            AcceptingNewClients = acceptingClients,
            NameContains = name,
        };

        Providers = await providerRepo.GetVisibleAsync(Filter);
        return Page();
    }

    public string GetPhotoUrl(string s3Key) => photoService.GetPhotoUrl(s3Key);
}
