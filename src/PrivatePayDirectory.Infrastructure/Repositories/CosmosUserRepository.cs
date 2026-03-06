using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Options;
using PrivatePayDirectory.Core.Enums;
using PrivatePayDirectory.Core.Interfaces;
using PrivatePayDirectory.Core.Models;

namespace PrivatePayDirectory.Infrastructure.Repositories;

public class CosmosUserRepository(CosmosClient cosmosClient, IOptions<CosmosOptions> options)
    : IUserRepository
{
    private static readonly CosmosLinqSerializerOptions LinqOptions = new()
    {
        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
    };

    private Container Container => cosmosClient
        .GetDatabase(options.Value.DatabaseName)
        .GetContainer(options.Value.UsersContainer);

    public async Task<AppUser?> GetByIdAsync(string userId)
    {
        try
        {
            var response = await Container.ReadItemAsync<AppUser>(
                userId, new PartitionKey(userId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<AppUser?> GetByEmailAsync(string email)
    {
        var query = Container.GetItemLinqQueryable<AppUser>(linqSerializerOptions: LinqOptions)
            .Where(u => u.Email == email.ToLowerInvariant())
            .ToFeedIterator();

        while (query.HasMoreResults)
        {
            var page = await query.ReadNextAsync();
            var first = page.FirstOrDefault();
            if (first != null) return first;
        }
        return null;
    }

    public async Task<IReadOnlyList<AppUser>> GetAllAsync()
    {
        var iterator = Container.GetItemLinqQueryable<AppUser>(linqSerializerOptions: LinqOptions).ToFeedIterator();
        var results = new List<AppUser>();
        while (iterator.HasMoreResults)
            results.AddRange(await iterator.ReadNextAsync());
        return results.OrderBy(u => u.Email).ToList();
    }

    public async Task SaveAsync(AppUser user)
    {
        user.Email = user.Email.ToLowerInvariant();
        await Container.UpsertItemAsync(user, new PartitionKey(user.UserId));
    }

    public async Task DeleteAsync(string userId)
    {
        await Container.DeleteItemAsync<AppUser>(userId, new PartitionKey(userId));
    }
}
