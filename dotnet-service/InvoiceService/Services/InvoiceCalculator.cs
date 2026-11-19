using InvoiceService.Models;

namespace InvoiceService.Services;

public interface IInvoiceCalculator
{
    InvoiceResponse Calculate(InvoiceRequest request);
}

public class InvoiceCalculator : IInvoiceCalculator
{
    
    private static decimal R(decimal v) =>
        Math.Round(v, 2, MidpointRounding.AwayFromZero);

    public InvoiceResponse Calculate(InvoiceRequest request)
    {
        
        var isInterState = !string.Equals(
            request.SellerStateCode, request.BuyerStateCode,
            StringComparison.OrdinalIgnoreCase);

        var response = new InvoiceResponse
        {
            InvoiceNumber = request.InvoiceNumber,
            IsInterState = isInterState
        };

        foreach (var item in request.Items)
        {
            var gross = R(item.Quantity * item.UnitPrice);
            var discount = R(gross * item.DiscountPercent / 100m);
            var taxable = gross - discount;
            var tax = R(taxable * item.GstRate / 100m);

            decimal cgst = 0, sgst = 0, igst = 0;
            if (isInterState)
            {
                igst = tax;
            }
            else
            {
                cgst = R(tax / 2m);
                sgst = tax - cgst;   
            }

            response.Lines.Add(new InvoiceLineResult
            {
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                GrossAmount = gross,
                DiscountAmount = discount,
                TaxableAmount = taxable,
                GstRate = item.GstRate,
                Cgst = cgst,
                Sgst = sgst,
                Igst = igst,
                LineTotal = taxable + tax
            });
        }

        response.Subtotal = response.Lines.Sum(l => l.GrossAmount);
        response.TotalDiscount = response.Lines.Sum(l => l.DiscountAmount);
        response.TaxableAmount = response.Lines.Sum(l => l.TaxableAmount);
        response.TotalCgst = response.Lines.Sum(l => l.Cgst);
        response.TotalSgst = response.Lines.Sum(l => l.Sgst);
        response.TotalIgst = response.Lines.Sum(l => l.Igst);
        response.TotalTax = response.TotalCgst + response.TotalSgst + response.TotalIgst;

      
        var exact = response.TaxableAmount + response.TotalTax;
        var rounded = Math.Round(exact, 0, MidpointRounding.AwayFromZero);
        response.RoundOff = rounded - exact;
        response.GrandTotal = rounded;

        return response;
    }
}