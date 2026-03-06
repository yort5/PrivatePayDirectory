using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using PrivatePayDirectory.Core.Interfaces;
using PrivatePayDirectory.Core.Models;
using TherapistModel = PrivatePayDirectory.Core.Models.Therapist;

namespace PrivatePayDirectory.Web.Pages;

public class DirectoryModel(ITherapistRepository therapistRepo, IPhotoService photoService) : PageModel
{
    public IReadOnlyList<TherapistModel> Therapists { get; private set; } = [];
    public TherapistFilter Filter { get; private set; } = new();

    public IReadOnlyList<string> States => Taxonomy.UnitedStates;
    public IReadOnlyList<string> Specialties => Taxonomy.Specialties;
    public IReadOnlyList<string> InsurancePlans => Taxonomy.InsurancePlans;
    public IReadOnlyList<string> Languages => Taxonomy.Languages;

    public List<SelectListItem> SessionTypeOptions =>
    [
        new SelectListItem("Virtual", "virtual"),
        new SelectListItem("In-Person", "inperson"),
        new SelectListItem("Both", "both"),
    ];

    public async Task OnGetAsync(
        string? sessionType,
        string? state,
        string? specialty,
        string? insurance,
        string? language,
        bool? acceptingClients,
        string? name)
    {
        Filter = new TherapistFilter
        {
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

        Therapists = await therapistRepo.GetVisibleAsync(Filter);
    }

    public string GetPhotoUrl(string s3Key) => photoService.GetPhotoUrl(s3Key);
}
