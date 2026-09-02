using FinanceDashboardApi.Entities;

namespace FinanceDashboardApi.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> FindByIdAsync(long id);
    Task<User?> FindByUsernameAsync(string username);
    Task<User?> FindByEmailAsync(string email);
    Task<User?> FindByGoogleSubAsync(string googleSub);
    Task<bool> UsernameExistsAsync(string username);
    Task<bool> EmailExistsAsync(string email);
    Task<User> CreateAsync(User user);
    Task LinkGoogleSubAsync(long userId, string googleSub);
}
