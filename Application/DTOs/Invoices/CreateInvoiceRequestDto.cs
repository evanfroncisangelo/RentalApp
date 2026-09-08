using RentalApp.Domain.Enums;

namespace RentalApp.Application.DTOs.Invoices;

public class CreateInvoiceRequestDto
{
    public int TenantId { get; set; }
    public int LeaseId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public string? Notes { get; set; }
    public List<CreateInvoiceItemRequestDto> Items { get; set; } = [];
}
