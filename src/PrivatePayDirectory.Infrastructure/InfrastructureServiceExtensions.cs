using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrivatePayDirectory.Core.Interfaces;
using PrivatePayDirectory.Infrastructure.Repositories;
using PrivatePayDirectory.Infrastructure.Serialization;
using PrivatePayDirectory.Infrastructure.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PrivatePayDirectory.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static readonly JsonSerializerOptions CosmosJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CosmosOptions>(configuration.GetSection("CosmosDb"));
        var cosmosOptions = configuration.GetSection("CosmosDb").Get<CosmosOptions>() ?? new();

        services.AddSingleton(_ => new CosmosClient(
            cosmosOptions.ConnectionString,
            new CosmosClientOptions
            {
                Serializer = new CosmosSystemTextJsonSerializer(CosmosJsonOptions)
            }));

        services.AddScoped<ITherapistRepository, CosmosTherapistRepository>();
        services.AddScoped<IUserRepository, CosmosUserRepository>();

        services.Configure<BlobStorageOptions>(configuration.GetSection("BlobStorage"));
        var blobOptions = configuration.GetSection("BlobStorage").Get<BlobStorageOptions>() ?? new();
        services.AddSingleton(_ => new BlobServiceClient(blobOptions.ConnectionString));
        services.AddScoped<IPhotoService, BlobPhotoService>();

        services.AddScoped<INotificationService, NullNotificationService>();

        return services;
    }

    /// <summary>
    /// Creates the Cosmos database/containers and Blob container if they don't exist.
    /// Call once at startup before handling requests.
    /// </summary>
    public static async Task EnsureResourcesAsync(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(InfrastructureServiceExtensions));

        // Cosmos containers
        var cosmosClient = services.GetRequiredService<CosmosClient>();
        var cosmosOptions = services.GetRequiredService<IOptions<CosmosOptions>>().Value;

        var dbResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(cosmosOptions.DatabaseName);
        var db = dbResponse.Database;

        await db.CreateContainerIfNotExistsAsync(
            new ContainerProperties(cosmosOptions.TherapistsContainer, "/id"));
        await db.CreateContainerIfNotExistsAsync(
            new ContainerProperties(cosmosOptions.UsersContainer, "/id"));

        // Blob container — non-fatal if emulator isn't running locally
        try
        {
            var blobService = services.GetRequiredService<BlobServiceClient>();
            var blobOptions = services.GetRequiredService<IOptions<BlobStorageOptions>>().Value;
            var containerClient = blobService.GetBlobContainerClient(blobOptions.ContainerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                "Blob Storage unavailable at startup (photo uploads will fail): {Message}", ex.Message);
        }
    }
}
