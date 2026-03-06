using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrivatePayDirectory.Core.Interfaces;
using TherapistModel = PrivatePayDirectory.Core.Models.Therapist;

namespace PrivatePayDirectory.Web.Pages.Admin;

public class TherapistsModel(
    ITherapistRepository therapistRepo,
    INotificationService notifications) : PageModel
{
    public IReadOnlyList<TherapistModel> Therapists { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Therapists = (await therapistRepo.GetAllAsync())
            .OrderBy(t => t.IsVisible)   // pending first
            .ThenBy(t => t.LastName)
            .ToList();
    }

    public async Task<IActionResult> OnPostAsync(string therapistId, bool isVisible)
    {
        await therapistRepo.SetVisibilityAsync(therapistId, isVisible);

        if (isVisible)
        {
            var therapist = await therapistRepo.GetByIdAsync(therapistId);
            if (therapist != null)
                await notifications.NotifyProfileApprovedAsync(therapist);
        }

        TempData["Success"] = isVisible
            ? "Profile approved and is now visible in the directory."
            : "Profile hidden from the directory.";

        return RedirectToPage();
    }
}
