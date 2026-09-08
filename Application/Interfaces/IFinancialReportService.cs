using RentalApp.Application.DTOs.Finance;

namespace RentalApp.Application.Interfaces;

public interface IFinancialReportService
{
    Task<MonthlyFinancialSummaryDto> GetMonthlySummaryAsync(int year, int month, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MonthlyFinancialHistoryItemDto>> GetMonthlyHistoryAsync(int monthsBack, CancellationToken cancellationToken = default);
}
