using System.Text.Json.Serialization;
using FluentValidation;
using InvoiceService.Middleware;
using InvoiceService.Services;
using InvoiceService.Validators;
using QuestPDF.Infrastructure;

// QuestPDF free (Community) license
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        // "quantity": "2" maathiri string numbers-um accept pannum
        o.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Dependency Injection (interface -> implementation)
builder.Services.AddValidatorsFromAssemblyContaining<InvoiceRequestValidator>();
builder.Services.AddScoped<IInvoiceCalculator, InvoiceCalculator>();
builder.Services.AddSingleton<IPdfProtector, NoOpPdfProtector>();
builder.Services.AddScoped<IInvoicePdfGenerator, InvoicePdfGenerator>();

var app = builder.Build();

// Swagger local/dev-la mattum
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// API key check (INTERNAL_API_KEY set pannina mattum)
app.UseMiddleware<ApiKeyMiddleware>();

// Health check (AWS load balancer use pannum)
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "invoice-service" }));

app.MapControllers();
app.Run();

public partial class Program { }