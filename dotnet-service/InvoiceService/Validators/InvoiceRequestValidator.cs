using FluentValidation;
using InvoiceService.Models;

namespace InvoiceService.Validators;

public class InvoiceItemValidator : AbstractValidator<InvoiceItemRequest>
{
    
    private static readonly decimal[] AllowedGstRates = { 0m, 5m, 12m, 18m, 28m };

    public InvoiceItemValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100);
        RuleFor(x => x.GstRate)
            .Must(r => AllowedGstRates.Contains(r))
            .WithMessage("GST rate must be one of 0, 5, 12, 18, 28.");
    }
}

public class InvoiceRequestValidator : AbstractValidator<InvoiceRequest>
{
    public InvoiceRequestValidator()
    {
        RuleFor(x => x.InvoiceNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(150);

        RuleFor(x => x.InvoiceDate)
            .Must(d => d <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .WithMessage("Invoice date cannot be in the future.");

        RuleFor(x => x.SellerStateCode).Matches(@"^\d{2}$")
            .WithMessage("State code must be 2 digits (GST state code).");
        RuleFor(x => x.BuyerStateCode).Matches(@"^\d{2}$")
            .WithMessage("State code must be 2 digits (GST state code).");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one item is required.")
            .Must(i => i.Count <= 500).WithMessage("Maximum 500 items allowed.");

        RuleForEach(x => x.Items).SetValidator(new InvoiceItemValidator());
    }
}