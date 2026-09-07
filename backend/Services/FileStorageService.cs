using FinanceDashboardApi.Common;
using FinanceDashboardApi.DTOs.Files;
using FinanceDashboardApi.Entities;
using FinanceDashboardApi.Repositories.Interfaces;
using FinanceDashboardApi.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FinanceDashboardApi.Services;

public class FileStorageService(
    IFileRepository repository,
    ITransactionRepository transactionRepository,
    FileStorageOptions storageOptions) : IFileStorageService
{
    public async Task<ServiceResult<FileResponseDto>> UploadAsync(long userId, long transactionId, IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return ServiceResult<FileResponseDto>.Fail(422, "Please choose a file to upload.");
        }

        var transaction = await transactionRepository.GetByIdAsync(transactionId, userId);
        if (transaction is null)
        {
            return ServiceResult<FileResponseDto>.Fail(404, "Transaction not found.");
        }

        Directory.CreateDirectory(storageOptions.StorageRoot);

        var storedFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var storedPath = Path.Combine(storageOptions.StorageRoot, storedFileName);

        try
        {
            await using (var destination = new FileStream(storedPath, FileMode.CreateNew, FileAccess.Write))
            {
                await file.CopyToAsync(destination);
            }
        }
        catch (IOException)
        {
            return ServiceResult<FileResponseDto>.Fail(503, "Could not save the file.");
        }

        var record = new UploadedFile
        {
            UserId = userId,
            TransactionId = transactionId,
            OriginalFileName = Path.GetFileName(file.FileName),
            StoredFileName = storedFileName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            SizeBytes = file.Length,
            UploadedAt = DateTimeOffset.UtcNow
        };

        try
        {
            await repository.CreateAsync(record);
        }
        catch (Exception ex) when (ex is NpgsqlException or DbUpdateException { InnerException: NpgsqlException })
        {
            File.Delete(storedPath);
            return ServiceResult<FileResponseDto>.Fail(503, "Could not save the file.");
        }

        return ServiceResult<FileResponseDto>.Ok(ToDto(record));
    }

    public async Task<ServiceResult<FileDownload>> DownloadAsync(long id, long userId)
    {
        UploadedFile? record;

        try
        {
            record = await repository.GetByIdAsync(id, userId);
        }
        catch (NpgsqlException)
        {
            return ServiceResult<FileDownload>.Fail(503, "Could not read the file.");
        }

        if (record is null)
        {
            return ServiceResult<FileDownload>.Fail(404, "File not found.");
        }

        var storedPath = Path.Combine(storageOptions.StorageRoot, record.StoredFileName);

        if (!File.Exists(storedPath))
        {
            return ServiceResult<FileDownload>.Fail(404, "File not found.");
        }

        var stream = new FileStream(storedPath, FileMode.Open, FileAccess.Read);
        return ServiceResult<FileDownload>.Ok(new FileDownload(stream, record.ContentType, record.OriginalFileName));
    }

    private static FileResponseDto ToDto(UploadedFile f) => new()
    {
        Id = f.Id,
        FileName = f.OriginalFileName,
        ContentType = f.ContentType,
        SizeBytes = f.SizeBytes,
        UploadedAt = f.UploadedAt.ToString("O")
    };
}
