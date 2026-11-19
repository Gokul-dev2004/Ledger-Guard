using InvoiceService.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace InvoiceService.Services;

// Encryption design team decide pannina appram, idha implement pannalaam.
// Ippo NoOp = PDF-a maathaama appadiye tharum.
public interface IPdfProtector
{
    byte[] Protect(byte[] pdfBytes);
}

public class NoOpPdfProtector : IPdfProtector
{
    public byte[] Protect(byte[] pdfBytes) => pdfBytes;
}

public interface IInvoicePdfGenerator
{
    byte[] Generate(InvoiceRequest request, InvoiceResponse result);
}

public class InvoicePdfGenerator : IInvoicePdfGenerator
{
    private readonly IPdfProtector _protector;

    public InvoicePdfGenerator(IPdfProtector protector)
    {
        _protector = protector;
    }

    public byte[] Generate(InvoiceRequest req, InvoiceResponse res)
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Text($"TAX INVOICE  #{req.InvoiceNumber}")
                    .FontSize(18).Bold();

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(4);
                    col.Item().Text($"Customer: {req.CustomerName}");
                    col.Item().Text($"Date: {req.InvoiceDate:dd-MM-yyyy}");
                    col.Item().Text(res.IsInterState
                        ? "Supply type: Inter-state (IGST)"
                        : "Supply type: Intra-state (CGST + SGST)");

                    // Items table
                    col.Item().PaddingTop(10).Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(4);
                            c.RelativeColumn(1);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(1);
                            c.RelativeColumn(2);
                        });

                        t.Header(h =>
                        {
                            h.Cell().Text("Item").Bold();
                            h.Cell().Text("Qty").Bold();
                            h.Cell().Text("Rate").Bold();
                            h.Cell().Text("Taxable").Bold();
                            h.Cell().Text("GST%").Bold();
                            h.Cell().Text("Total").Bold();
                        });

                        foreach (var l in res.Lines)
                        {
                            t.Cell().Text(l.Description);
                            t.Cell().Text(l.Quantity.ToString("0.##"));
                            t.Cell().Text(l.UnitPrice.ToString("N2"));
                            t.Cell().Text(l.TaxableAmount.ToString("N2"));
                            t.Cell().Text(l.GstRate.ToString("0.##"));
                            t.Cell().Text(l.LineTotal.ToString("N2"));
                        }
                    });

                    // Totals
                    col.Item().PaddingTop(15).AlignRight().Column(s =>
                    {
                        s.Item().Text($"Subtotal: {res.Subtotal:N2}");
                        s.Item().Text($"Discount: -{res.TotalDiscount:N2}");
                        s.Item().Text($"Taxable: {res.TaxableAmount:N2}");
                        if (res.IsInterState)
                        {
                            s.Item().Text($"IGST: {res.TotalIgst:N2}");
                        }
                        else
                        {
                            s.Item().Text($"CGST: {res.TotalCgst:N2}");
                            s.Item().Text($"SGST: {res.TotalSgst:N2}");
                        }
                        s.Item().Text($"Round off: {res.RoundOff:N2}");
                        s.Item().Text($"GRAND TOTAL: {res.GrandTotal:N2}")
                            .Bold().FontSize(13);
                    });
                });
            });
        }).GeneratePdf();

        return _protector.Protect(bytes);
    }
}