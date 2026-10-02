using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrivatePayDirectory.Core.Enums;
using PrivatePayDirectory.Core.Interfaces;
using PrivatePayDirectory.Core.Models;

namespace PrivatePayDirectory.Web.Pages.Admin;

public class UsersModel(IUserRepository userRepo) : PageModel
{
    public IReadOnlyList<AppUser> Users { get; private set; } = [];
    public IReadOnlyList<string> AllRoles { get; } = Enum.GetNames<UserRole>();
    public IReadOnlyList<Profession> AllProfessions { get; } = Enum.GetValues<Profession>();

    public async Task OnGetAsync()
    {
        Users = await userRepo.GetAllAsync();
    }

    public async Task<IActionResult> OnPostAsync(string userId, string role, List<string>? managedProfessions)
    {
        var user = await userRepo.GetByIdAsync(userId);
        if (user == null) return NotFound();

        if (Enum.TryParse<UserRole>(role, out var parsedRole))
        {
            user.Role = parsedRole;

            if (parsedRole == UserRole.Administrator && managedProfessions is { Count: > 0 })
            {
                user.ManagedProfessions = managedProfessions
                    .Select(p => Enum.TryParse<Profession>(p, out var parsed) ? (Profession?)parsed : null)
                    .Where(p => p.HasValue)
                    .Select(p => p!.Value)
                    .ToList();
                if (user.ManagedProfessions.Count == 0) user.ManagedProfessions = null;
            }
            else
            {
                user.ManagedProfessions = null;
            }

            await userRepo.SaveAsync(user);
            TempData["Success"] = $"Updated {user.Email} to {role}.";
        }

        return RedirectToPage();
    }
}
