using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrivatePayDirectory.Core.Interfaces;
using TherapistModel = PrivatePayDirectory.Core.Models.Therapist;

namespace PrivatePayDirectory.Web.Pages.Therapist;

public class ProfileModel(ITherapistRepository therapistRepo, IPhotoService photoService) : PageModel
{
    public TherapistModel Therapist { get; private set; } = null!;
    public string? PhotoUrl { get; private set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var therapist = await therapistRepo.GetByIdAsync(id);
        if (therapist == null)
            return NotFound();

        // Non-admins can only see visible profiles
        if (!therapist.IsVisible && !User.IsInRole("Administrator"))
            return NotFound();

        Therapist = therapist;
        PhotoUrl = therapist.ProfilePhotoKey != null
            ? photoService.GetPhotoUrl(therapist.ProfilePhotoKey)
            : null;

        return Page();
    }
}
