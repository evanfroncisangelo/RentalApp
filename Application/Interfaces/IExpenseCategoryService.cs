using RentalApp.Application.DTOs.ExpenseCategories;

namespace RentalApp.Application.Interfaces;

public interface IExpenseCategoryService
{
    Task<IReadOnlyList<ExpenseCategoryDto>> GetAllAsync(string? search, CancellationToken cancellationToken = default);
    Task<ExpenseCategoryDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ExpenseCategoryDto> CreateAsync(CreateExpenseCategoryRequestDto request, CancellationToken cancellationToken = default);
    Task<ExpenseCategoryDto> UpdateAsync(int id, UpdateExpenseCategoryRequestDto request, CancellationToken cancellationToken = default);
    Task DeactivateAsync(int id, CancellationToken cancellationToken = default);
}
