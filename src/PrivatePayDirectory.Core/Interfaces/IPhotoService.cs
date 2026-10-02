namespace PrivatePayDirectory.Core.Interfaces;

public interface IPhotoService
{
    /// <summary>The blob key a provider's profile photo is stored under.</summary>
    string GetPhotoKey(string providerId);

    /// <summary>Uploads a photo stream server-side and returns the blob key.</summary>
    Task<string> UploadPhotoAsync(string providerId, Stream content, string contentType);

    /// <summary>Generates a pre-signed PUT URL for the client to upload a photo directly to blob storage.</summary>
    Task<string> GenerateUploadUrlAsync(string providerId, string contentType);

    /// <summary>Returns a URL suitable for displaying the provider's photo.</summary>
    string GetPhotoUrl(string s3Key);

    /// <summary>Deletes a provider's photo from blob storage.</summary>
    Task DeletePhotoAsync(string s3Key);
}
