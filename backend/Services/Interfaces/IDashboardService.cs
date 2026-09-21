using FinanceDashboardApi.Common;
using FinanceDashboardApi.DTOs.Dashboard;

namespace FinanceDashboardApi.Services.Interfaces;

public interface IDashboardService
{
    Task<ServiceResult<DashboardSummaryDto>> GetDashboardAsync(long userId, int year);
}
