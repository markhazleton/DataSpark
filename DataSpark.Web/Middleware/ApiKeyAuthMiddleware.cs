using System.Text.Json;

namespace DataSpark.Web.Middleware;

/// <summary>
/// Enforces API key authentication on API routes.
/// </summary>
public sealed class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiKeyAuthMiddleware> _logger;

    public ApiKeyAuthMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ApiKeyAuthMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headerName = _configuration["ApiKey:HeaderName"] ?? "X-Api-Key";
        var configuredApiKey = _configuration["ApiKey:Key"];

        if (string.IsNullOrWhiteSpace(configuredApiKey))
        {
            _logger.LogError("API key is not configured. Rejecting request for {Path}", context.Request.Path);
            await WriteUnauthorizedAsync(context, "UNAUTHORIZED", "API key is not configured.").ConfigureAwait(false);
            return;
        }

        if (!context.Request.Headers.TryGetValue(headerName, out var providedKey) ||
            string.IsNullOrWhiteSpace(providedKey) ||
            !string.Equals(providedKey.ToString(), configuredApiKey, StringComparison.Ordinal))
        {
            await WriteUnauthorizedAsync(context, "UNAUTHORIZED", "Missing or invalid API key.").ConfigureAwait(false);
            return;
        }

        await _next(context).ConfigureAwait(false);
    }

    private static async Task WriteUnauthorizedAsync(HttpContext context, string code, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status = "error",
            error = new { code, message },
            meta = new { timestamp = DateTime.UtcNow, requestId = context.TraceIdentifier }
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload)).ConfigureAwait(false);
    }
}
