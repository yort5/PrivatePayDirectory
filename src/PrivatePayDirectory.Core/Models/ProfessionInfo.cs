using PrivatePayDirectory.Core.Enums;

namespace PrivatePayDirectory.Core.Models;

/// <summary>Per-profession presentation: URL slug, names, and the copy for that profession's directory page.</summary>
/// <param name="Icon">Material Symbols icon name.</param>
public sealed record ProfessionInfo(
    Profession Profession,
    string Slug,
    string DisplayName,
    string PluralName,
    string Headline,
    string Tagline,
    string Icon);
