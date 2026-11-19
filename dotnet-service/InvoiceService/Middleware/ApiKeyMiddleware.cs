using System.Security.Cryptography;
using System.Text;

namespace InvoiceService.Middleware;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _apiKey;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next = next;
        _apiKey = config["INTERNAL_API_KEY"] ?? string.Empty;
    }

    public async Task Invoke(HttpContext ctx)
    {
        var path = ctx.Request.Path;
        var isOpen = path.StartsWithSegments("/health") || path.StartsWithSegments("/swagger");

        // Key set pannala-na (local dev) check skip aagum
        if (isOpen || string.IsNullOrEmpty(_apiKey))
        {
            await _next(ctx);
            return;
        }

        var provided = ctx.Request.Headers["X-Internal-Key"].ToString();
        var a = Encoding.UTF8.GetBytes(provided);
        var b = Encoding.UTF8.GetBytes(_apiKey);

        // Fixed-time compare: timing attack-ku safe
        if (a.Length != b.Length || !CryptographicOperations.FixedTimeEquals(a, b))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await ctx.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
            return;
        }

        await _next(ctx);
    }
}