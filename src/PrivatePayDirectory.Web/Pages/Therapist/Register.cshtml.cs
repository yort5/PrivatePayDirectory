using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

        // Already has a therapist profile — send straight to Edit
        if (user?.TherapistId != null)
            return RedirectToPage("/Therapist/Edit");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await userRepo.GetByIdAsync(userId);
        if (user == null) return Unauthorized();

        if (user.TherapistId != null)
            return RedirectToPage("/Therapist/Edit");

        var therapist = new TherapistModel
        {
            UserId = userId,
            IsVisible = false,
            Email = user.Email,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            InsuranceAccepted = ["Private Pay"],
        };

        await therapistRepo.SaveAsync(therapist);

        // Save TherapistId on the user — role stays Standard until Admin approves
        user.TherapistId = therapist.TherapistId;
        await userRepo.SaveAsync(user);

        await notifications.NotifyProfilePendingReviewAsync(therapist);

        // Re-issue the cookie so the TherapistId claim is available in the nav immediately
        var claims = User.Claims.Where(c => c.Type != "TherapistId").ToList();
        claims.Add(new Claim("TherapistId", therapist.TherapistId));
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });

        return RedirectToPage("/Therapist/Edit");
    }
}
