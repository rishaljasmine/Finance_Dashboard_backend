using FinanceDashboardApi.Data;
using FinanceDashboardApi.Entities;
using FinanceDashboardApi.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinanceDashboardApi.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> FindByIdAsync(long id) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> FindByUsernameAsync(string username) =>
        db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

    public Task<User?> FindByEmailAsync(string email) =>
        db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

    public Task<User?> FindByGoogleSubAsync(string googleSub) =>
        db.Users.FirstOrDefaultAsync(u => u.GoogleSub == googleSub);

    public Task<bool> UsernameExistsAsync(string username) =>
        db.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower());

    public Task<bool> EmailExistsAsync(string email) =>
        db.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());

    public async Task<User> CreateAsync(User user)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public Task LinkGoogleSubAsync(long userId, string googleSub) =>
        db.Users.Where(u => u.Id == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.GoogleSub, googleSub));
}
