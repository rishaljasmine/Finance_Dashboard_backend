using FinanceDashboardApi.Common;
using FinanceDashboardApi.DTOs.Auth;
using FinanceDashboardApi.DTOs.Users;

namespace FinanceDashboardApi.Services.Interfaces;

public interface IAuthService
{
    Task<ServiceResult<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request);
    Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginRequestDto request);
    Task<ServiceResult<AuthResponseDto>> GoogleLoginAsync(string credential);
    Task<ServiceResult<UserResponseDto>> GetCurrentUserAsync(long userId);
}
