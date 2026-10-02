using System.Security.Claims;
using PrivatePayDirectory.Core.Enums;
using PrivatePayDirectory.Core.Interfaces;

namespace PrivatePayDirectory.Web;

public static class AdminScope
{
    /// <summary>
    /// Returns the professions the signed-in admin is limited to, or null if unscoped (can manage all).
    /// Read from the user document rather than a claim so scope changes apply without re-login.
    /// </summary>
    public static async Task<List<Profession>?> GetAdminScopeAsync(this IUserRepository userRepo, ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return null;
        var adminUser = await userRepo.GetByIdAsync(userId);
        return adminUser?.ManagedProfessions is { Count: > 0 } scope ? scope : null;
    }
}
