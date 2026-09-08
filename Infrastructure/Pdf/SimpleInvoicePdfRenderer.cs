using System.Text;
using RentalApp.Application.DTOs.Invoices;
using RentalApp.Application.Interfaces;

namespace RentalApp.Infrastructure.Pdf;

public class SimpleInvoicePdfRenderer : IInvoicePdfRenderer
{
    public byte[] Render(InvoiceDto invoice)
    {
        var lines = new List<string>
        {
            "Rental Invoice",
            $"Invoice Number: {invoice.InvoiceNumber}",
            $"Tenant: {invoice.TenantName}",
            $"Unit: {invoice.UnitNumber}",
            $"Invoice Date: {invoice.InvoiceDate:yyyy-MM-dd}",
            $"Due Date: {invoice.DueDate:yyyy-MM-dd}",
            "",
            "Items:"
        };

        lines.AddRange(invoice.Items.Select(i => $"- {i.Description}: {i.Amount:N2}"));
        lines.Add("");
        lines.Add($"Total: {invoice.Total:N2}");

        var escapedText = string.Join("\\n", lines.Select(EscapePdfText));
        var contentStream = $"BT /F1 12 Tf 50 780 Td 14 TL ({escapedText}) Tj ET";

        var objects = new List<string>
        {
            "1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj",
            "2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj",
            "3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >> endobj",
            "4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj",
            $"5 0 obj << /Length {Encoding.ASCII.GetByteCount(contentStream)} >> stream\n{contentStream}\nendstream endobj"
        };

        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, Encoding.ASCII, leaveOpen: true);

        writer.WriteLine("%PDF-1.4");
        writer.Flush();

        var offsets = new List<long> { 0 };
        foreach (var obj in objects)
        {
            offsets.Add(ms.Position);
            writer.WriteLine(obj);
            writer.Flush();
        }

        var xrefPosition = ms.Position;
        writer.WriteLine($"xref\n0 {objects.Count + 1}");
        writer.WriteLine("0000000000 65535 f ");

        for (var i = 1; i < offsets.Count; i++)
        {
            writer.WriteLine($"{offsets[i]:D10} 00000 n ");
        }

        writer.WriteLine($"trailer << /Size {objects.Count + 1} /Root 1 0 R >>");
        writer.WriteLine("startxref");
        writer.WriteLine(xrefPosition);
        writer.Write("%%EOF");
        writer.Flush();

        return ms.ToArray();
    }

    private static string EscapePdfText(string input)
        => input.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
}
