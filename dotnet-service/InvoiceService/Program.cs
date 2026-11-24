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
if (!string.IsNullOrEmpty(builder.Configuration["KMS_KEY_ID"]))
{
    // Production: AWS KMS
    builder.Services.AddSingleton<Amazon.KeyManagementService.IAmazonKeyManagementService>(
        _ => new Amazon.KeyManagementService.AmazonKeyManagementServiceClient());
    builder.Services.AddSingleton<IPdfProtector, KmsPdfProtector>();
}
else if (builder.Environment.IsDevelopment()
         && !string.IsNullOrEmpty(builder.Configuration["LOCAL_DEV_ENC_KEY"]))
{
    // Local testing only
    builder.Services.AddSingleton<IPdfProtector, LocalAesPdfProtector>();
}
else
{
    builder.Services.AddSingleton<IPdfProtector, NoOpPdfProtector>();
}builder.Services.AddScoped<IInvoicePdfGenerator, InvoicePdfGenerator>();

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