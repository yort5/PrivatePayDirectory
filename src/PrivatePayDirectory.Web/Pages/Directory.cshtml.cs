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

    public IReadOnlyList<string> States => Taxonomy.UnitedStates;
    public IReadOnlyList<string> Specialties => Taxonomy.SpecialtiesByProfession[Profession.Profession];
    public IReadOnlyList<string> InsurancePlans => Taxonomy.InsurancePlans;
    public IReadOnlyList<string> Languages => Taxonomy.Languages;

    /// <summary>The other professions, for the cross-promotion section.</summary>
    public IEnumerable<ProfessionInfo> OtherProfessions =>
        Taxonomy.Professions.Where(p => p.Profession != Profession.Profession);

    public List<SelectListItem> SessionTypeOptions =>
    [
        new SelectListItem("Virtual", "virtual"),
        new SelectListItem("In-Person", "inperson"),
        new SelectListItem("Both", "both"),
    ];

    public async Task<IActionResult> OnGetAsync(
        string slug,
        string? sessionType,
        string? state,
        string? specialty,
        string? insurance,
        string? language,
        bool? acceptingClients,
        string? name)
    {
        // The route constraint already guarantees a known slug
        Profession = Taxonomy.FindBySlug(slug)!;

        Filter = new ProviderFilter
        {
            Profession = Profession.Profession,
            OffersVirtual = sessionType is "virtual" or "both" ? true : null,
            OffersInPerson = sessionType is "inperson" or "both" ? true : null,
            VirtualState = sessionType is "virtual" or "both" ? state : null,
            OfficeState = sessionType is "inperson" or "both" ? state : (sessionType == null ? state : null),
            Specialty = specialty,
            Insurance = insurance,
            Language = language,
            AcceptingNewClients = acceptingClients,
            NameContains = name,
        };

        Providers = await providerRepo.GetVisibleAsync(Filter);
        return Page();
    }

    public string GetPhotoUrl(string s3Key) => photoService.GetPhotoUrl(s3Key);
}
