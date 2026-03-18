using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrivatePayDirectory.Core.Enums;
using PrivatePayDirectory.Core.Interfaces;
using TherapistModel = PrivatePayDirectory.Core.Models.Therapist;

namespace PrivatePayDirectory.Web.Pages.Admin;

public class TherapistsModel(
    ITherapistRepository therapistRepo,
    IUserRepository userRepo,
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

        var therapist = await therapistRepo.GetByIdAsync(therapistId);
        if (therapist != null)
        {
            if (isVisible)
            {
                // Grant Therapist role on approval so role-gated features work on next login
                var user = await userRepo.GetByIdAsync(therapist.UserId);
                if (user != null && user.Role == UserRole.Standard)
                {
                    user.Role = UserRole.Therapist;
                    await userRepo.SaveAsync(user);
                }

                await notifications.NotifyProfileApprovedAsync(therapist);
            }
            else
            {
                // Revoke Therapist role when hiding (back to Standard)
                var user = await userRepo.GetByIdAsync(therapist.UserId);
                if (user != null && user.Role == UserRole.Therapist)
                {
                    user.Role = UserRole.Standard;
                    await userRepo.SaveAsync(user);
                }
            }
        }

        TempData["Success"] = isVisible
            ? "Profile approved and is now visible in the directory."
            : "Profile hidden from the directory.";

        return RedirectToPage();
    }
}
