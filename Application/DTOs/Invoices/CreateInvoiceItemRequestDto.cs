namespace RentalApp.Application.DTOs.Invoices;

public class CreateInvoiceItemRequestDto
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
