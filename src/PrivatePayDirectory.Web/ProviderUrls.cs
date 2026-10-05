using System.Globalization;
using System.Text;
using PrivatePayDirectory.Core.Interfaces;
using PrivatePayDirectory.Core.Models;

namespace PrivatePayDirectory.Web;

/// <summary>Readable public profile URLs: /{profession-slug}/{provider-slug}, e.g. /therapists/grace-fischer-austin.</summary>
public static class ProviderUrls
{
    /// <summary>Public path for a profile. Falls back to the id route for profiles not yet given a slug.</summary>
    public static string ProfilePath(Provider p) =>
        p.Slug != null
            ? $"/{Taxonomy.Info(p.Profession).Slug}/{p.Slug}"
            : $"/Provider/Profile/{p.ProviderId}";

    /// <summary>Sets <see cref="Provider.Slug"/> from name + first office city, numbering it if another profile has it.</summary>
    public static async Task AssignSlugAsync(IProviderRepository repo, Provider p)
    {
        var baseSlug = Slugify($"{p.FirstName} {p.LastName} {p.Offices.FirstOrDefault()?.City}");
        if (baseSlug.Length == 0) baseSlug = "provider";

        var candidate = baseSlug;
        for (var n = 2; await repo.GetBySlugAsync(candidate) is { } other && other.ProviderId != p.ProviderId; n++)
            candidate = $"{baseSlug}-{n}";
        p.Slug = candidate;
    }

    /// <summary>Lowercase ASCII words joined by hyphens: "José O'Brien (Example)" → "jose-obrien-example".</summary>
    public static string Slugify(string text)
    {
        var sb = new StringBuilder();
        var pendingHyphen = false;
        foreach (var c in text.Normalize(NormalizationForm.FormD).ToLowerInvariant())
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark || c == '\'') continue;
            if (c is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                if (pendingHyphen && sb.Length > 0) sb.Append('-');
                sb.Append(c);
                pendingHyphen = false;
            }
            else
            {
                pendingHyphen = true;
            }
        }
        return sb.ToString();
    }
}
