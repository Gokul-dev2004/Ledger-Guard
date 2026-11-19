namespace InvoiceService.Models;

public class InvoiceItemRequest
{
    public string Description { get; set; } = string.Empty;
    public string? HsnCode { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }   // 0 - 100
    public decimal GstRate { get; set; }           // 0, 5, 12, 18, 28
}

public class InvoiceRequest
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string SellerStateCode { get; set; } = string.Empty;  // e.g. "33" = Tamil Nadu
    public string BuyerStateCode { get; set; } = string.Empty;   // e.g. "29" = Karnataka
    public List<InvoiceItemRequest> Items { get; set; } = new();
}

public class InvoiceLineResult
{
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal GstRate { get; set; }
    public decimal Cgst { get; set; }
    public decimal Sgst { get; set; }
    public decimal Igst { get; set; }
    public decimal LineTotal { get; set; }
}

public class InvoiceResponse
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public bool IsInterState { get; set; }
    public List<InvoiceLineResult> Lines { get; set; } = new();
    public decimal Subtotal { get; set; }         
    public decimal TotalDiscount { get; set; }
    public decimal TaxableAmount { get; set; }    
    public decimal TotalCgst { get; set; }
    public decimal TotalSgst { get; set; }
    public decimal TotalIgst { get; set; }
    public decimal TotalTax { get; set; }
    public decimal RoundOff { get; set; }
    public decimal GrandTotal { get; set; }
}