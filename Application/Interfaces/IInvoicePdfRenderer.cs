using RentalApp.Application.DTOs.Invoices;

namespace RentalApp.Application.Interfaces;

public interface IInvoicePdfRenderer
{
    byte[] Render(InvoiceDto invoice);
}
