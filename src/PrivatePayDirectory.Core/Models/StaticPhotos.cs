namespace PrivatePayDirectory.Core.Models;

/// <summary>
/// Photos shipped with the site (the example profiles' dog photos) rather than uploaded to storage.
/// Their photo key is the site-relative path itself, served as-is and never deleted.
/// </summary>
public static class StaticPhotos
{
    public const string Prefix = "/images/demo/";

    /// <summary>Only keys inside the demo folder count, so a provider can't point their photo at an arbitrary URL.</summary>
    public static bool IsStatic(string key) =>
        key.StartsWith(Prefix, StringComparison.Ordinal) && !key.Contains("..");
}
