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

    public async Task OnGetAsync()
    {
        Users = await userRepo.GetAllAsync();
    }

    public async Task<IActionResult> OnPostAsync(string userId, string role)
    {
        var user = await userRepo.GetByIdAsync(userId);
        if (user == null) return NotFound();

        if (Enum.TryParse<UserRole>(role, out var parsed))
        {
            user.Role = parsed;
            await userRepo.SaveAsync(user);
            TempData["Success"] = $"Updated {user.Email} to {role}.";
        }

        return RedirectToPage();
    }
}
