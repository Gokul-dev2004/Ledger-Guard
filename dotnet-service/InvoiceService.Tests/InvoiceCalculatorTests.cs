using InvoiceService.Models;
using InvoiceService.Services;
using Xunit;

namespace InvoiceService.Tests;

public class InvoiceCalculatorTests
{
    private static InvoiceRequest Sample(string buyerState) => new()
    {
        InvoiceNumber = "INV-001",
        InvoiceDate = new DateOnly(2026, 11, 17),
        CustomerName = "Company A",
        SellerStateCode = "33",
        BuyerStateCode = buyerState,
        Items = new()
        {
            new() { Description = "A", Quantity = 2, UnitPrice = 1000, DiscountPercent = 10, GstRate = 18 },
            new() { Description = "B", Quantity = 1, UnitPrice = 500, DiscountPercent = 0, GstRate = 5 }
        }
    };

    [Fact]
    public void IntraState_SplitsCgstSgst()
    {
        var r = new InvoiceCalculator().Calculate(Sample("33"));

        Assert.False(r.IsInterState);
        Assert.Equal(2500m, r.Subtotal);
        Assert.Equal(200m, r.TotalDiscount);
        Assert.Equal(2300m, r.TaxableAmount);
        Assert.Equal(174.5m, r.TotalCgst);
        Assert.Equal(174.5m, r.TotalSgst);
        Assert.Equal(0m, r.TotalIgst);
        Assert.Equal(2649m, r.GrandTotal);
    }

    [Fact]
    public void InterState_UsesIgst()
    {
        var r = new InvoiceCalculator().Calculate(Sample("29"));

        Assert.True(r.IsInterState);
        Assert.Equal(349m, r.TotalIgst);
        Assert.Equal(0m, r.TotalCgst);
        Assert.Equal(0m, r.TotalSgst);
        Assert.Equal(2649m, r.GrandTotal);
    }

    [Fact]
    public void ZeroGst_NoTax()
    {
        var req = Sample("33");
        foreach (var i in req.Items) i.GstRate = 0;

        var r = new InvoiceCalculator().Calculate(req);

        Assert.Equal(0m, r.TotalTax);
        Assert.Equal(2300m, r.GrandTotal);
    }

    [Fact]
    public void FullDiscount_TotalIsZero()
    {
        var req = Sample("33");
        foreach (var i in req.Items) i.DiscountPercent = 100;

        var r = new InvoiceCalculator().Calculate(req);

        Assert.Equal(0m, r.TaxableAmount);
        Assert.Equal(0m, r.TotalTax);
        Assert.Equal(0m, r.GrandTotal);
    }

    [Fact]
    public void OddTax_CgstPlusSgstEqualsTax()
    {
        var req = new InvoiceRequest
        {
            InvoiceNumber = "INV-002",
            InvoiceDate = new DateOnly(2026, 11, 17),
            CustomerName = "Company A",
            SellerStateCode = "33",
            BuyerStateCode = "33",
            Items = new()
            {
                new() { Description = "X", Quantity = 1, UnitPrice = 10.10m, DiscountPercent = 0, GstRate = 5 }
            }
        };

        var r = new InvoiceCalculator().Calculate(req);

        Assert.Equal(r.TotalTax, r.TotalCgst + r.TotalSgst);
        Assert.Equal(0.51m, r.TotalTax);
    }
}