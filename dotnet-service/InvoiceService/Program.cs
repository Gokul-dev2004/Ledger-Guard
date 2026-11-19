using System.Text.Json.Serialization;
using FluentValidation;
using InvoiceService.Middleware;
using InvoiceService.Services;
using InvoiceService.Validators;
using QuestPDF.Infrastructure;


QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
      
        o.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddValidatorsFromAssemblyContaining<InvoiceRequestValidator>();
builder.Services.AddScoped<IInvoiceCalculator, InvoiceCalculator>();
builder.Services.AddSingleton<IPdfProtector, NoOpPdfProtector>();
builder.Services.AddScoped<IInvoicePdfGenerator, InvoicePdfGenerator>();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseMiddleware<ApiKeyMiddleware>();


app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "invoice-service" }));

app.MapControllers();
app.Run();

public partial class Program { }