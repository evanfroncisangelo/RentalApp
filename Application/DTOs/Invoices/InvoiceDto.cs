using RentalApp.Domain.Enums;

namespace RentalApp.Application.DTOs.Invoices;

public class InvoiceDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public int LeaseId { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public InvoiceStatus Status { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyList<InvoiceItemDto> Items { get; set; } = [];
}
