using FinanceDashboardApi.Entities;

namespace FinanceDashboardApi.Repositories.Interfaces;

public interface IFileRepository
{
    Task<UploadedFile> CreateAsync(UploadedFile file);
    Task<UploadedFile?> GetByIdAsync(long id, long userId);
}
