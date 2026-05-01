using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrivatePayDirectory.Core.Enums;
using PrivatePayDirectory.Core.Interfaces;
using PrivatePayDirectory.Web;
using TherapistModel = PrivatePayDirectory.Core.Models.Therapist;

namespace PrivatePayDirectory.Web.Pages.Admin;

[Authorize(Policy = Policies.RequireAdmin)]
public class TherapistsModel(
    ITherapistRepository therapistRepo,
    IUserRepository userRepo,
    IPhotoService photoService,
    INotificationService notifications) : PageModel
{
    public IReadOnlyList<TherapistModel> Pending { get; private set; } = [];
    public IReadOnlyList<TherapistModel> Approved { get; private set; } = [];

    public string GetPhotoUrl(string key) => photoService.GetPhotoUrl(key);

    public async Task OnGetAsync()
    {
        var all = (await therapistRepo.GetAllAsync())
            .OrderBy(t => t.CreatedAt)
            .ToList();

        Pending = all.Where(t => !t.IsVisible).ToList();
        Approved = all.Where(t => t.IsVisible).OrderBy(t => t.LastName).ToList();
    }

    public async Task<IActionResult> OnPostAsync(string therapistId, bool isVisible)
    {
        await therapistRepo.SetVisibilityAsync(therapistId, isVisible);

        var therapist = await therapistRepo.GetByIdAsync(therapistId);
        if (therapist != null)
        {
            if (isVisible)
            {
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
                var user = await userRepo.GetByIdAsync(therapist.UserId);
                if (user != null && user.Role == UserRole.Therapist)
                {
                    user.Role = UserRole.Standard;
                    await userRepo.SaveAsync(user);
                }
            }
        }

        TempData["Success"] = isVisible
            ? $"{therapist?.FirstName} {therapist?.LastName}''s profile is now live in the directory."
            : $"{therapist?.FirstName} {therapist?.LastName}''s profile has been hidden.";

        return RedirectToPage();
    }
}
