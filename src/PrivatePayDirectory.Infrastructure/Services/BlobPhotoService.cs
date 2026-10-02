using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using PrivatePayDirectory.Core.Interfaces;

namespace PrivatePayDirectory.Infrastructure.Services;

public class BlobPhotoService(BlobServiceClient blobServiceClient, IOptions<BlobStorageOptions> options)
    : IPhotoService
{
    private readonly BlobStorageOptions _options = options.Value;

    private BlobContainerClient ContainerClient =>
        blobServiceClient.GetBlobContainerClient(_options.ContainerName);

    public async Task<string> UploadPhotoAsync(string providerId, Stream content, string contentType)
    {
        var blobName = GetPhotoKey(providerId);
        var blobClient = ContainerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        });
        return blobName;
    }

    public async Task<string> GenerateUploadUrlAsync(string providerId, string contentType)
    {
        var blobName = GetPhotoKey(providerId);
        var blobClient = ContainerClient.GetBlobClient(blobName);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _options.ContainerName,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(15),
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);

        var sasUri = blobClient.GenerateSasUri(sasBuilder);
        return sasUri.ToString();
    }

    public string GetPhotoUrl(string blobName)
    {
        var blobClient = ContainerClient.GetBlobClient(blobName);
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _options.ContainerName,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);
        return blobClient.GenerateSasUri(sasBuilder).ToString();
    }

    public async Task DeletePhotoAsync(string s3Key)
    {
        var blobClient = ContainerClient.GetBlobClient(s3Key);
        await blobClient.DeleteIfExistsAsync();
    }

    public string GetPhotoKey(string providerId) =>
        $"providers/{providerId}/profile";
}

public class BlobStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "provider-photos";
}
