namespace PrivatePayDirectory.Core.Interfaces;

public interface IPhotoService
{
    /// <summary>Generates a pre-signed PUT URL for the client to upload a photo directly to S3.</summary>
    Task<string> GenerateUploadUrlAsync(string therapistId, string contentType);

    /// <summary>Returns a URL suitable for displaying the therapist's photo.</summary>
    string GetPhotoUrl(string s3Key);

    /// <summary>Deletes the photo for a therapist from S3.</summary>
    Task DeletePhotoAsync(string s3Key);
}
