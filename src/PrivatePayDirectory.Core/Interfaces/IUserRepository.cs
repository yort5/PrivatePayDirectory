using PrivatePayDirectory.Core.Models;

namespace PrivatePayDirectory.Core.Interfaces;

public interface IUserRepository
{
    Task<AppUser?> GetByIdAsync(string userId);
    Task<AppUser?> GetByEmailAsync(string email);
    Task<IReadOnlyList<AppUser>> GetAllAsync();
    Task SaveAsync(AppUser user);
    Task DeleteAsync(string userId);
}
