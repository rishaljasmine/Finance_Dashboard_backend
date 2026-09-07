using FinanceDashboardApi.Data;
using FinanceDashboardApi.Entities;
using FinanceDashboardApi.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinanceDashboardApi.Repositories;

public class FileRepository(AppDbContext db) : IFileRepository
{
    public async Task<UploadedFile> CreateAsync(UploadedFile file)
    {
        db.Files.Add(file);
        await db.SaveChangesAsync();
        return file;
    }

    public Task<UploadedFile?> GetByIdAsync(long id, long userId) =>
        db.Files.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
}
