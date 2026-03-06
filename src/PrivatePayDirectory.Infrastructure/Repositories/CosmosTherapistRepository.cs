using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Options;
using PrivatePayDirectory.Core.Interfaces;
using PrivatePayDirectory.Core.Models;

namespace PrivatePayDirectory.Infrastructure.Repositories;

public class CosmosTherapistRepository(CosmosClient cosmosClient, IOptions<CosmosOptions> options)
    : ITherapistRepository
{
    private static readonly CosmosLinqSerializerOptions LinqOptions = new()
    {
        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
    };

    private Container Container => cosmosClient
        .GetDatabase(options.Value.DatabaseName)
        .GetContainer(options.Value.TherapistsContainer);

    public async Task<Therapist?> GetByIdAsync(string therapistId)
    {
        try
        {
            var response = await Container.ReadItemAsync<Therapist>(
                therapistId, new PartitionKey(therapistId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Therapist?> GetByUserIdAsync(string userId)
    {
        var query = Container.GetItemLinqQueryable<Therapist>(linqSerializerOptions: LinqOptions)
            .Where(t => t.UserId == userId)
            .ToFeedIterator();

        return await ReadFirstOrDefaultAsync(query);
    }

    public async Task<IReadOnlyList<Therapist>> GetVisibleAsync(TherapistFilter? filter = null)
    {
        var queryable = Container.GetItemLinqQueryable<Therapist>(linqSerializerOptions: LinqOptions)
            .Where(t => t.IsVisible);

        if (filter?.AcceptingNewClients == true)
            queryable = queryable.Where(t => t.AcceptingNewClients);

        var therapists = await ReadAllAsync(queryable.ToFeedIterator());
        return ApplyInMemoryFilter(therapists, filter);
    }

    public async Task<IReadOnlyList<Therapist>> GetAllAsync()
    {
        var iterator = Container.GetItemLinqQueryable<Therapist>(linqSerializerOptions: LinqOptions).ToFeedIterator();
        var results = await ReadAllAsync(iterator);
        return results.OrderBy(t => t.LastName).ThenBy(t => t.FirstName).ToList();
    }

    public async Task SaveAsync(Therapist therapist)
    {
        therapist.UpdatedAt = DateTime.UtcNow;
        await Container.UpsertItemAsync(therapist, new PartitionKey(therapist.TherapistId));
    }

    public async Task SetVisibilityAsync(string therapistId, bool isVisible)
    {
        var therapist = await GetByIdAsync(therapistId);
        if (therapist == null) return;
        therapist.IsVisible = isVisible;
        await SaveAsync(therapist);
    }

    public async Task DeleteAsync(string therapistId)
    {
        await Container.DeleteItemAsync<Therapist>(therapistId, new PartitionKey(therapistId));
    }

    private static IReadOnlyList<Therapist> ApplyInMemoryFilter(List<Therapist> therapists, TherapistFilter? filter)
    {
        if (filter == null) return therapists.OrderBy(t => t.LastName).ThenBy(t => t.FirstName).ToList();

        IEnumerable<Therapist> q = therapists;

        if (filter.OffersVirtual == true)
            q = q.Where(t => t.OffersVirtual);
        if (filter.OffersInPerson == true)
            q = q.Where(t => t.OffersInPerson);
        if (!string.IsNullOrWhiteSpace(filter.VirtualState))
            q = q.Where(t => t.LicensedVirtualStates.Contains(filter.VirtualState, StringComparer.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(filter.OfficeState))
            q = q.Where(t => t.Offices.Any(o => string.Equals(o.State, filter.OfficeState, StringComparison.OrdinalIgnoreCase)));
        if (!string.IsNullOrWhiteSpace(filter.Specialty))
            q = q.Where(t => t.Specialties.Contains(filter.Specialty, StringComparer.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(filter.Insurance))
            q = q.Where(t => t.InsuranceAccepted.Contains(filter.Insurance, StringComparer.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(filter.Language))
            q = q.Where(t => t.Languages.Contains(filter.Language, StringComparer.OrdinalIgnoreCase));
        if (filter.AcceptingNewClients.HasValue)
            q = q.Where(t => t.AcceptingNewClients == filter.AcceptingNewClients.Value);
        if (!string.IsNullOrWhiteSpace(filter.NameContains))
            q = q.Where(t => (t.FirstName + " " + t.LastName).Contains(filter.NameContains, StringComparison.OrdinalIgnoreCase));

        return q.OrderBy(t => t.LastName).ThenBy(t => t.FirstName).ToList();
    }

    private static async Task<List<Therapist>> ReadAllAsync(FeedIterator<Therapist> iterator)
    {
        var results = new List<Therapist>();
        while (iterator.HasMoreResults)
            results.AddRange(await iterator.ReadNextAsync());
        return results;
    }

    private static async Task<Therapist?> ReadFirstOrDefaultAsync(FeedIterator<Therapist> iterator)
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
