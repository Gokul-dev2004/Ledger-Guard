# Invoice Service (.NET)

Stateless invoice calculation and PDF service for LedgerGuard.
Called by the Node.js main backend over REST/JSON. No database.

- Framework: ASP.NET Core Web API, .NET 9
- Port (local): 8080

## Authentication

Every request (except `/health` and `/swagger`) needs this header
when `INTERNAL_API_KEY` is set on the server:

```
X-Internal-Key: <shared secret>
```

## Endpoints

| Method | Endpoint                | Purpose                    |
|--------|-------------------------|----------------------------|
| GET    | /health                 | Health check (no key)      |
| POST   | /api/invoices/validate  | Validate invoice payload   |
| POST   | /api/invoices/calculate | Calculate totals and GST   |
| POST   | /api/invoices/pdf       | Generate invoice PDF       |

## Request

```json
{
  "invoiceNumber": "INV-001",
  "invoiceDate": "2026-11-17",
  "customerName": "Company A",
  "sellerStateCode": "33",
  "buyerStateCode": "33",
  "items": [
    {
      "description": "Laptop Stand",
      "quantity": 2,
      "unitPrice": 1000,
      "discountPercent": 10,
      "gstRate": 18
    }
  ]
}
```

| Field | Type | Rule |
|---|---|---|
| invoiceDate | string | `YYYY-MM-DD`, not in the future |
| sellerStateCode / buyerStateCode | string | exactly 2 digits, always a string ("09", not 9) |
| quantity | number | greater than 0 |
| unitPrice | number | 0 or more |
| discountPercent | number | 0 to 100 |
| gstRate | number | one of 0, 5, 12, 18, 28 |
| items | array | 1 to 500 items |

## GST rule

- Same seller and buyer state: CGST + SGST (half each)
- Different state: IGST (full rate)

## Response (calculate)

```json
{
  "invoiceNumber": "INV-001",
  "isInterState": false,
  "lines": [],
  "subtotal": 2500,
  "totalDiscount": 200,
  "taxableAmount": 2300,
  "totalCgst": 174.5,
  "totalSgst": 174.5,
  "totalIgst": 0,
  "totalTax": 349,
  "roundOff": 0,
  "grandTotal": 2649
}
```

For analytics: revenue = `grandTotal`, tax = `totalTax`.

## Error format (400)

```json
{ "errors": [ { "propertyName": "Items[0].GstRate", "errorMessage": "GST rate must be one of 0, 5, 12, 18, 28." } ] }
```

## Environment variables

| Name | Purpose |
|---|---|
| INTERNAL_API_KEY | Shared secret with the Node backend (never commit it) |
| ASPNETCORE_URLS | Default `http://+:8080` in Docker |

## Run locally

```
cd InvoiceService
dotnet run --urls http://localhost:8080
```

## Run tests

```
dotnet test
```