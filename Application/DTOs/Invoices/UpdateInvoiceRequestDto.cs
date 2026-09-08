using RentalApp.Domain.Enums;

namespace RentalApp.Application.DTOs.Invoices;

public class UpdateInvoiceRequestDto
{
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public InvoiceStatus Status { get; set; }
    public string? Notes { get; set; }
    public List<CreateInvoiceItemRequestDto> Items { get; set; } = [];
}
