using RentalApp.Application.DTOs.Dashboard;

namespace RentalApp.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(int year, CancellationToken cancellationToken = default);
}
