namespace RentalApp.Application.DTOs.Invoices;

public class GenerateInvoiceRequestDto
{
    public int LeaseId { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Notes { get; set; }
}
