using PrivatePayDirectory.Core.Interfaces;

namespace PrivatePayDirectory.Infrastructure.Local;

/// <summary>
/// Stores photos on disk in Local mode. The web app serves them from <see cref="UrlPrefix"/>.
/// </summary>
public class LocalPhotoService(string rootPath) : IPhotoService
{
    public const string UrlPrefix = "/local-photos/";

    public async Task<string> UploadPhotoAsync(string providerId, Stream content, string contentType)
    {
        var key = GetPhotoKey(providerId);
        var path = ResolvePath(key)!;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using (var file = File.Create(path))
            await content.CopyToAsync(file);
        await File.WriteAllTextAsync(path + ".contenttype", contentType);
        return key;
    }

    public string GetPhotoUrl(string key)
    {
        // Version by write time so a replaced photo isn't served from browser cache
        var path = ResolvePath(key);
        var version = path != null && File.Exists(path) ? File.GetLastWriteTimeUtc(path).Ticks : 0;
        return $"{UrlPrefix}{key}?v={version}";
    }

    public Task DeletePhotoAsync(string key)
    {
        var path = ResolvePath(key);
        if (path != null)
        {
            File.Delete(path);
            File.Delete(path + ".contenttype");
        }
        return Task.CompletedTask;
    }

    /// <summary>Opens a stored photo for serving, or returns null if it doesn't exist.</summary>
    public (Stream Content, string ContentType)? Open(string key)
    {
        var path = ResolvePath(key);
        if (path == null || !File.Exists(path)) return null;
        var typePath = path + ".contenttype";
        var contentType = File.Exists(typePath) ? File.ReadAllText(typePath) : "application/octet-stream";
        return (File.OpenRead(path), contentType);
    }

    private static string GetPhotoKey(string providerId) =>
        $"providers/{providerId}/profile";
    // Keys come from URLs when serving, so refuse anything that escapes the photo root
    private string? ResolvePath(string key)
    {
        var root = Path.GetFullPath(rootPath);
        var path = Path.GetFullPath(Path.Combine(root, key));
        return path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal) ? path : null;
    }
}
