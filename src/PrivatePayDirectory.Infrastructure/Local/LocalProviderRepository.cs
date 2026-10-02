using PrivatePayDirectory.Core.Interfaces;
using PrivatePayDirectory.Core.Models;
using PrivatePayDirectory.Infrastructure.Repositories;

namespace PrivatePayDirectory.Infrastructure.Local;

public class LocalProviderRepository(LocalJsonStore<Provider> store) : IProviderRepository
{
    public Task<Provider?> GetByIdAsync(string providerId) =>
        Task.FromResult(store.Get(providerId));

    public Task<Provider?> GetByUserIdAsync(string userId) =>
        Task.FromResult(store.GetAll().FirstOrDefault(p => p.UserId == userId));

    public Task<IReadOnlyList<Provider>> GetVisibleAsync(ProviderFilter? filter = null) =>
        Task.FromResult(ProviderFiltering.Apply(store.GetAll().Where(p => p.IsVisible), filter));

    public Task<IReadOnlyList<Provider>> GetAllAsync() =>
        Task.FromResult(ProviderFiltering.Apply(store.GetAll(), null));

    public Task SaveAsync(Provider provider)
    {
        provider.UpdatedAt = DateTime.UtcNow;
        store.Upsert(provider);
        return Task.CompletedTask;
    }

    public Task SetVisibilityAsync(string providerId, bool isVisible)
    {
        var provider = store.Get(providerId);
        if (provider == null) return Task.CompletedTask;
        provider.IsVisible = isVisible;
        return SaveAsync(provider);
    }

    public Task DeleteAsync(string providerId)
    {
        store.Delete(providerId);
        return Task.CompletedTask;
    }
}
