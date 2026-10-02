using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using PrivatePayDirectory.Core.Enums;
using PrivatePayDirectory.Core.Interfaces;
using PrivatePayDirectory.Core.Models;
using ProviderModel = PrivatePayDirectory.Core.Models.Provider;

namespace PrivatePayDirectory.Web.Pages;

public class DirectoryModel(IProviderRepository providerRepo, IPhotoService photoService) : PageModel
{
    public IReadOnlyList<ProviderModel> Providers { get; private set; } = [];
    public ProviderFilter Filter { get; private set; } = new();

    public IReadOnlyList<string> States => Taxonomy.UnitedStates;

    // Specialties scoped to the selected profession, or all specialties when no profession selected
    public IReadOnlyList<string> Specialties => Filter.Profession.HasValue
        ? Taxonomy.SpecialtiesByProfession.TryGetValue(Filter.Profession.Value, out var list) ? list : []
        : Taxonomy.AllSpecialties;

    public IReadOnlyList<string> InsurancePlans => Taxonomy.InsurancePlans;
    public IReadOnlyList<string> Languages => Taxonomy.Languages;
    public IReadOnlyList<(Profession Value, string Display)> ProfessionOptions =>
        Enum.GetValues<Profession>().Select(p => (p, Taxonomy.ProfessionDisplay[p])).ToList();

    public List<SelectListItem> SessionTypeOptions =>
    [
        new SelectListItem("Virtual", "virtual"),
        new SelectListItem("In-Person", "inperson"),
        new SelectListItem("Both", "both"),
    ];

    public async Task OnGetAsync(
        string? profession,
        string? sessionType,
        string? state,
        string? specialty,
        string? insurance,
        string? language,
        bool? acceptingClients,
        string? name)
    {
        Profession? professionFilter = Enum.TryParse<Profession>(profession, out var parsedProfession)
            ? parsedProfession
            : null;

        Filter = new ProviderFilter
        {
            Profession = professionFilter,
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
    }

    public string GetPhotoUrl(string s3Key) => photoService.GetPhotoUrl(s3Key);
}
