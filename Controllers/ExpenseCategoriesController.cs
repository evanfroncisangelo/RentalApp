using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalApp.Application.DTOs.ExpenseCategories;
using RentalApp.Application.Interfaces;

namespace RentalApp.Controllers;

[ApiController]
[ApiAuthorize]
[Route("api/[controller]")]
public class ExpenseCategoriesController(IExpenseCategoryService expenseCategoryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExpenseCategoryDto>>> GetAll([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var items = await expenseCategoryService.GetAllAsync(search, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExpenseCategoryDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await expenseCategoryService.GetByIdAsync(id, cancellationToken);
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseCategoryDto>> Create([FromBody] CreateExpenseCategoryRequestDto request, CancellationToken cancellationToken)
    {
        var created = await expenseCategoryService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ExpenseCategoryDto>> Update(int id, [FromBody] UpdateExpenseCategoryRequestDto request, CancellationToken cancellationToken)
    {
        var updated = await expenseCategoryService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await expenseCategoryService.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }
}

