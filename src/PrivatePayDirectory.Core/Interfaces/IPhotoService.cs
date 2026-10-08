namespace PrivatePayDirectory.Core.Interfaces;

/// <remarks>Keys for photos shipped with the site (see <see cref="Models.StaticPhotos"/>) are served as-is and never deleted.</remarks>
public interface IPhotoService
{
    /// <summary>Uploads a photo stream server-side and returns the blob key.</summary>
    Task<string> UploadPhotoAsync(string providerId, Stream content, string contentType);

    /// <summary>Returns a URL suitable for displaying the provider's photo.</summary>
    string GetPhotoUrl(string s3Key);

    /// <summary>Deletes a provider's photo from blob storage.</summary>
    Task DeletePhotoAsync(string s3Key);
}
