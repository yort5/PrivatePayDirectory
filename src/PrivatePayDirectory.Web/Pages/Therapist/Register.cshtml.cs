using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrivatePayDirectory.Core.Enums;
using PrivatePayDirectory.Core.Interfaces;
using System.Security.Claims;
using TherapistModel = PrivatePayDirectory.Core.Models.Therapist;

namespace PrivatePayDirectory.Web.Pages.Therapist;

public class RegisterModel(
    ITherapistRepository therapistRepo,
    IUserRepository userRepo,
    INotificationService notifications) : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await userRepo.GetByIdAsync(userId);

        if (user?.Role == UserRole.Therapist)
            return RedirectToPage("/Therapist/Edit");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await userRepo.GetByIdAsync(userId);
        if (user == null) return Unauthorized();

        if (user.Role == UserRole.Therapist)
            return RedirectToPage("/Therapist/Edit");

        var therapist = new TherapistModel
        {
            UserId = userId,
            IsVisible = false,
            Email = user.Email,
        };

        await therapistRepo.SaveAsync(therapist);

        user.Role = UserRole.Therapist;
        user.TherapistId = therapist.TherapistId;
        await userRepo.SaveAsync(user);

        await notifications.NotifyProfilePendingReviewAsync(therapist);

        return RedirectToPage("/Therapist/Edit");
    }
}
