using RentalApp.Application.DTOs.Expenses;

namespace RentalApp.Application.Interfaces;

public interface IExpenseService
{
    Task<IReadOnlyList<ExpenseDto>> GetAllAsync(DateTime? fromDate, DateTime? toDate, int? categoryId, CancellationToken cancellationToken = default);
    Task<ExpenseDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ExpenseDto> CreateAsync(CreateExpenseRequestDto request, CancellationToken cancellationToken = default);
    Task<ExpenseDto> UpdateAsync(int id, UpdateExpenseRequestDto request, CancellationToken cancellationToken = default);
}
