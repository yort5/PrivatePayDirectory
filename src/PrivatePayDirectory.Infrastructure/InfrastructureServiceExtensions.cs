using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrivatePayDirectory.Core.Interfaces;
using PrivatePayDirectory.Core.Models;
using PrivatePayDirectory.Infrastructure.Local;
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
        var storageSection = configuration.GetSection("Storage");
        services.Configure<StorageOptions>(storageSection);
        var storageOptions = storageSection.Get<StorageOptions>() ?? new();

        if (storageOptions.IsLocal)
            AddLocalStorage(services, storageOptions);
        else
            AddAzureStorage(services, configuration);

        services.AddScoped<INotificationService, NullNotificationService>();

        return services;
    }

    private static void AddAzureStorage(IServiceCollection services, IConfiguration configuration)
    {
        var cosmosSection = configuration.GetSection("Cosmos");
        if (!cosmosSection.Exists())
            cosmosSection = configuration.GetSection("CosmosDb");

        services.Configure<CosmosOptions>(cosmosSection);
        var cosmosOptions = cosmosSection.Get<CosmosOptions>() ?? new();
        cosmosOptions.ConnectionString = BuildCosmosConnectionString(cosmosOptions);

        services.AddSingleton(_ => new CosmosClient(
            cosmosOptions.ConnectionString,
            new CosmosClientOptions
            {
                Serializer = new CosmosSystemTextJsonSerializer(CosmosJsonOptions)
            }));

        services.AddScoped<IProviderRepository, CosmosProviderRepository>();
        services.AddScoped<IUserRepository, CosmosUserRepository>();

        services.Configure<BlobStorageOptions>(configuration.GetSection("BlobStorage"));
        var blobOptions = configuration.GetSection("BlobStorage").Get<BlobStorageOptions>() ?? new();
        services.AddSingleton(_ => new BlobServiceClient(blobOptions.ConnectionString));
        services.AddScoped<IPhotoService, BlobPhotoService>();
    }

    private static void AddLocalStorage(IServiceCollection services, StorageOptions storageOptions)
    {
        var root = Path.GetFullPath(storageOptions.LocalDataPath);
        services.AddSingleton(new LocalJsonStore<Provider>(Path.Combine(root, "providers.json"), p => p.ProviderId));
        services.AddSingleton(new LocalJsonStore<AppUser>(Path.Combine(root, "users.json"), u => u.UserId));
        services.AddSingleton<IProviderRepository, LocalProviderRepository>();
        services.AddSingleton<IUserRepository, LocalUserRepository>();

        var photoService = new LocalPhotoService(Path.Combine(root, "photos"));
        services.AddSingleton(photoService);
        services.AddSingleton<IPhotoService>(photoService);
    }

    /// <summary>
    /// Creates the Cosmos database/containers and Blob container if they don't exist (no-op in Local mode).
    /// Call once at startup before handling requests.
    /// </summary>
    public static async Task EnsureResourcesAsync(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(InfrastructureServiceExtensions));

        var storageOptions = services.GetRequiredService<IOptions<StorageOptions>>().Value;
        if (storageOptions.IsLocal)
        {
            logger.LogInformation(
                "Local storage mode — using files under '{Path}' instead of Cosmos DB / Blob Storage.",
                Path.GetFullPath(storageOptions.LocalDataPath));
            return;
        }

        // Cosmos containers
        var cosmosClient = services.GetRequiredService<CosmosClient>();
        var cosmosOptions = services.GetRequiredService<IOptions<CosmosOptions>>().Value;

        logger.LogInformation(
            "Ensuring Cosmos resources. Database='{DatabaseName}', ProvidersContainer='{ProvidersContainer}', UsersContainer='{UsersContainer}', HasConnectionString={HasConnectionString}, HasAccountEndpoint={HasAccountEndpoint}",
            cosmosOptions.DatabaseName,
            cosmosOptions.ProvidersContainer,
            cosmosOptions.UsersContainer,
            !string.IsNullOrWhiteSpace(cosmosOptions.ConnectionString),
            !string.IsNullOrWhiteSpace(cosmosOptions.AccountEndpoint));

        var dbResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(cosmosOptions.DatabaseName);
        var db = dbResponse.Database;

        await db.CreateContainerIfNotExistsAsync(
            new ContainerProperties(cosmosOptions.ProvidersContainer, "/id"));
        await db.CreateContainerIfNotExistsAsync(
            new ContainerProperties(cosmosOptions.UsersContainer, "/id"));

        // Blob container — non-fatal if emulator isn't running locally
        try
        {
            var blobService = services.GetRequiredService<BlobServiceClient>();
            var blobOptions = services.GetRequiredService<IOptions<BlobStorageOptions>>().Value;
            logger.LogInformation(
                "Ensuring Blob container. Container='{ContainerName}', HasConnectionString={HasConnectionString}",
                blobOptions.ContainerName,
                !string.IsNullOrWhiteSpace(blobOptions.ConnectionString));
            var containerClient = blobService.GetBlobContainerClient(blobOptions.ContainerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                "Blob Storage unavailable at startup (photo uploads will fail): {Message}", ex.Message);
        }
    }

    private static string BuildCosmosConnectionString(CosmosOptions cosmosOptions)
    {
        if (!string.IsNullOrWhiteSpace(cosmosOptions.ConnectionString))
            return cosmosOptions.ConnectionString;

        if (!string.IsNullOrWhiteSpace(cosmosOptions.AccountEndpoint) &&
            !string.IsNullOrWhiteSpace(cosmosOptions.AccountKey))
        {
            return $"AccountEndpoint={cosmosOptions.AccountEndpoint};AccountKey={cosmosOptions.AccountKey}";
        }

        throw new InvalidOperationException(
            "Cosmos DB configuration is missing. Set Cosmos:ConnectionString or Cosmos:AccountEndpoint + Cosmos:AccountKey.");
    }
}
