using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrivatePayDirectory.Core.Enums;
using PrivatePayDirectory.Core.Interfaces;
using PrivatePayDirectory.Core.Models;
using System.Security.Claims;

namespace PrivatePayDirectory.Web.Pages.Provider;

public class RegisterModel(
    IProviderRepository providerRepo,
    IUserRepository userRepo,
    INotificationService notifications) : PageModel
{
    [BindProperty]
    public Profession Profession { get; set; } = Profession.Therapist;

    public IEnumerable<(Profession Value, string Display)> ProfessionOptions =>
        Enum.GetValues<Profession>().Select(p => (p, Taxonomy.ProfessionDisplay[p]));

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await userRepo.GetByIdAsync(userId);

        if (user?.ProviderId != null)
            return RedirectToPage("/Provider/Edit");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await userRepo.GetByIdAsync(userId);
        if (user == null) return Unauthorized();

        if (user.ProviderId != null)
            return RedirectToPage("/Provider/Edit");

        var provider = new Core.Models.Provider
        {
            UserId = userId,
            IsVisible = false,
            Profession = Profession,
            Email = user.Email,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            InsuranceAccepted = ["Private Pay"],
        };

        await providerRepo.SaveAsync(provider);

        // Save ProviderId on the user — role stays Standard until Admin approves
        user.ProviderId = provider.ProviderId;
        await userRepo.SaveAsync(user);

        await notifications.NotifyProfilePendingReviewAsync(provider);

        // Re-issue the cookie so the ProviderId claim is available in the nav immediately
        var claims = User.Claims.Where(c => c.Type != "ProviderId").ToList();
        claims.Add(new Claim("ProviderId", provider.ProviderId));
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });

        return RedirectToPage("/Provider/Edit");
    }
}
