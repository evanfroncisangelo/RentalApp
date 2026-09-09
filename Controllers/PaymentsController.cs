using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalApp.Application.DTOs.Payments;
using RentalApp.Application.Interfaces;

namespace RentalApp.Controllers;

[ApiController]
[ApiAuthorize]
[Route("api/[controller]")]
public class PaymentsController(IPaymentService paymentService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PaymentDto>>> GetAll([FromQuery] int? leaseId, CancellationToken cancellationToken)
    {
        var items = await paymentService.GetAllAsync(leaseId, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PaymentDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await paymentService.GetByIdAsync(id, cancellationToken);
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<PaymentDto>> Create([FromBody] CreatePaymentRequestDto request, CancellationToken cancellationToken)
    {
        var created = await paymentService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PaymentDto>> Update(int id, [FromBody] UpdatePaymentRequestDto request, CancellationToken cancellationToken)
    {
        var updated = await paymentService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }
}

