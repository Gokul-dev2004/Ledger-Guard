using FluentValidation;
using InvoiceService.Models;
using InvoiceService.Services;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceService.Controllers;

[ApiController]
[Route("api/invoices")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceCalculator _calculator;
    private readonly IInvoicePdfGenerator _pdf;
    private readonly IValidator<InvoiceRequest> _validator;

    private readonly IPdfProtector _protector;

    public InvoicesController(
        IInvoiceCalculator calculator,
        IInvoicePdfGenerator pdf,
        IValidator<InvoiceRequest> validator,
        IPdfProtector protector)
    {
        _calculator = calculator;
        _pdf = pdf;
        _validator = validator;
        _protector = protector;
    }

   
    [HttpPost("validate")]
    public async Task<IActionResult> Validate(InvoiceRequest request)
    {
        var result = await _validator.ValidateAsync(request);
        return Ok(new
        {
            isValid = result.IsValid,
            errors = result.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
        });
    }

    
    [HttpPost("calculate")]
    public async Task<IActionResult> Calculate(InvoiceRequest request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(new
            {
                errors = validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            });
        }

        return Ok(_calculator.Calculate(request));
    }

   
    [HttpPost("pdf")]
    public async Task<IActionResult> GeneratePdf(InvoiceRequest request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(new
            {
                errors = validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            });
        }

        var result = _calculator.Calculate(request);
        var bytes = await _pdf.GenerateAsync(request, result);

        return _protector.IsEncrypted
            ? File(bytes, "application/octet-stream", $"invoice-{request.InvoiceNumber}.pdf.enc")
            : File(bytes, "application/pdf", $"invoice-{request.InvoiceNumber}.pdf");
    }
    [HttpPost("pdf/decrypt")]
    public async Task<IActionResult> DecryptPdf(IFormFile file)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var plain = await _protector.UnprotectAsync(ms.ToArray());
        return File(plain, "application/pdf", "invoice.pdf");
    }
}