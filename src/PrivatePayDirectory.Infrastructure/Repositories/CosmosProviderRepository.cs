using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Options;
using PrivatePayDirectory.Core.Interfaces;
using PrivatePayDirectory.Core.Models;

namespace PrivatePayDirectory.Infrastructure.Repositories;

public class CosmosProviderRepository(CosmosClient cosmosClient, IOptions<CosmosOptions> options)
    : IProviderRepository
{
    private static readonly CosmosLinqSerializerOptions LinqOptions = new()
    {
        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
    };

    private Container Container => cosmosClient
        .GetDatabase(options.Value.DatabaseName)
        .GetContainer(options.Value.ProvidersContainer);

    public async Task<Provider?> GetByIdAsync(string providerId)
    {
        try
        {
            var response = await Container.ReadItemAsync<Provider>(
                providerId, new PartitionKey(providerId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Provider?> GetByUserIdAsync(string userId)
    {
        var query = Container.GetItemLinqQueryable<Provider>(linqSerializerOptions: LinqOptions)
            .Where(p => p.UserId == userId)
            .ToFeedIterator();

        return await ReadFirstOrDefaultAsync(query);
    }

    public async Task<IReadOnlyList<Provider>> GetVisibleAsync(ProviderFilter? filter = null)
    {
        var queryable = Container.GetItemLinqQueryable<Provider>(linqSerializerOptions: LinqOptions)
            .Where(p => p.IsVisible);

        if (filter?.Profession is { } profession)
            queryable = queryable.Where(p => p.Profession == profession);
        if (filter?.AcceptingNewClients == true)
            queryable = queryable.Where(p => p.AcceptingNewClients);

        var providers = await ReadAllAsync(queryable.ToFeedIterator());
        return ProviderFiltering.Apply(providers, filter);
    }

    public async Task<IReadOnlyList<Provider>> GetAllAsync()
    {
        var iterator = Container.GetItemLinqQueryable<Provider>(linqSerializerOptions: LinqOptions).ToFeedIterator();
        var results = await ReadAllAsync(iterator);
        return results.OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ToList();
    }

    public async Task SaveAsync(Provider provider)
    {
        provider.UpdatedAt = DateTime.UtcNow;
        await Container.UpsertItemAsync(provider, new PartitionKey(provider.ProviderId));
    }

    public async Task SetVisibilityAsync(string providerId, bool isVisible)
    {
        var provider = await GetByIdAsync(providerId);
        if (provider == null) return;
        provider.IsVisible = isVisible;
        await SaveAsync(provider);
    }

    public async Task DeleteAsync(string providerId)
    {
        await Container.DeleteItemAsync<Provider>(providerId, new PartitionKey(providerId));
    }

    private static async Task<List<Provider>> ReadAllAsync(FeedIterator<Provider> iterator)
    {
        var results = new List<Provider>();
        while (iterator.HasMoreResults)
            results.AddRange(await iterator.ReadNextAsync());
        return results;
    }

    private static async Task<Provider?> ReadFirstOrDefaultAsync(FeedIterator<Provider> iterator)
    {
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync();
            var first = page.FirstOrDefault();
            if (first != null) return first;
        }
        return null;
    }
}
