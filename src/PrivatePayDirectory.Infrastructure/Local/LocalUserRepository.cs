using PrivatePayDirectory.Core.Interfaces;
using PrivatePayDirectory.Core.Models;

namespace PrivatePayDirectory.Infrastructure.Local;

public class LocalUserRepository(LocalJsonStore<AppUser> store) : IUserRepository
{
    public Task<AppUser?> GetByIdAsync(string userId) =>
        Task.FromResult(store.Get(userId));

    public Task<AppUser?> GetByEmailAsync(string email) =>
        Task.FromResult(store.GetAll().FirstOrDefault(u => u.Email == email.ToLowerInvariant()));

    public Task<IReadOnlyList<AppUser>> GetAllAsync() =>
        Task.FromResult<IReadOnlyList<AppUser>>(store.GetAll().OrderBy(u => u.Email).ToList());

    public Task SaveAsync(AppUser user)
    {
        user.Email = user.Email.ToLowerInvariant();
        store.Upsert(user);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string userId)
    {
        store.Delete(userId);
        return Task.CompletedTask;
    }
}
