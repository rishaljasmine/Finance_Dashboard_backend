using System.Security.Claims;
using FinanceDashboardApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceDashboardApi.Controllers;

[Authorize]
[ApiController]
[Route("/api/files")]
public class FilesController(IFileStorageService fileStorageService) : ControllerBase
{
    private long UserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("{id:long}/download")]
    public async Task<IActionResult> Download(long id)
    {
        var result = await fileStorageService.DownloadAsync(id, UserId);
        if (!result.Success)
        {
            return Problem(statusCode: result.ErrorStatusCode, detail: result.ErrorMessage);
        }

        var download = result.Data!;
        return File(download.Content, download.ContentType, download.FileName);
    }
}
