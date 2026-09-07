using FinanceDashboardApi.Common;
using FinanceDashboardApi.DTOs.Files;
using Microsoft.AspNetCore.Http;

namespace FinanceDashboardApi.Services.Interfaces;

public interface IFileStorageService
{
    Task<ServiceResult<FileResponseDto>> UploadAsync(long userId, long transactionId, IFormFile? file);
    Task<ServiceResult<FileDownload>> DownloadAsync(long id, long userId);
}

public record FileDownload(Stream Content, string ContentType, string FileName);
