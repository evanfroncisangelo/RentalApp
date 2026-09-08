using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalApp.Application.DTOs.Invoices;
using RentalApp.Application.Interfaces;

namespace RentalApp.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class InvoicesController(IInvoiceService invoiceService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InvoiceDto>>> GetAll(CancellationToken cancellationToken)
    {
        var items = await invoiceService.GetAllAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InvoiceDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await invoiceService.GetByIdAsync(id, cancellationToken);
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceDto>> Create([FromBody] CreateInvoiceRequestDto request, CancellationToken cancellationToken)
    {
        var created = await invoiceService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<InvoiceDto>> Update(int id, [FromBody] UpdateInvoiceRequestDto request, CancellationToken cancellationToken)
    {
        var updated = await invoiceService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpPost("generate")]
    public async Task<ActionResult<InvoiceDto>> Generate([FromBody] GenerateInvoiceRequestDto request, CancellationToken cancellationToken)
    {
        var invoice = await invoiceService.GenerateFromLeaseAsync(request, cancellationToken);
        return Ok(invoice);
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> DownloadPdf(int id, CancellationToken cancellationToken)
    {
        var bytes = await invoiceService.GeneratePdfAsync(id, cancellationToken);
        return File(bytes, "application/pdf", $"invoice-{id}.pdf");
    }
}
