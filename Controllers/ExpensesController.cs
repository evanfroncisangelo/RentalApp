using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalApp.Application.DTOs.Expenses;
using RentalApp.Application.Interfaces;

namespace RentalApp.Controllers;

[ApiController]
[ApiAuthorize]
[Route("api/[controller]")]
public class ExpensesController(IExpenseService expenseService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExpenseDto>>> GetAll([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] int? categoryId, CancellationToken cancellationToken)
    {
        var items = await expenseService.GetAllAsync(fromDate, toDate, categoryId, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExpenseDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await expenseService.GetByIdAsync(id, cancellationToken);
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseDto>> Create([FromBody] CreateExpenseRequestDto request, CancellationToken cancellationToken)
    {
        var created = await expenseService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ExpenseDto>> Update(int id, [FromBody] UpdateExpenseRequestDto request, CancellationToken cancellationToken)
    {
        var updated = await expenseService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }
}

