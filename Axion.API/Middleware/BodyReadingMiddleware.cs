using System.Text.Json;

namespace Axion.API.Middleware;

public class BodyReadingMiddleware(RequestDelegate next, ILogger<BodyReadingMiddleware> logger)
{
    private const string ParsedBodyJsonElementKey = "ParsedBodyJsonElement";
    private const string RawBodyStringKey = "RawBodyString";

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.ContentLength > 0 && context.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
        {
            try
            {
                context.Request.EnableBuffering();
                using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                var bodyString = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
                if (!string.IsNullOrWhiteSpace(bodyString))
                {
                    context.Items[RawBodyStringKey] = bodyString;
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(bodyString);
                        var jsonElement = jsonDoc.RootElement.Clone();
                        context.Items[ParsedBodyJsonElementKey] = jsonElement;
                        
                        logger.LogDebug("Request body parsed and cached. Size: {Size} bytes", bodyString.Length);
                    }
                    catch (JsonException ex)
                    {
                        logger.LogWarning(ex, "Failed to parse request body as JSON. Body will be stored as string only.");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error reading request body");
            }
        }

        await next(context);
    }
    
    public static JsonElement? GetJsonBody(HttpContext context)
    {
        if (context.Items.TryGetValue(ParsedBodyJsonElementKey, out var value) && value is JsonElement jsonElement)
        {
            return jsonElement;
        }
        
        return null;
    }
    
    public static string? GetRawBody(HttpContext context)
    {
        if (context.Items.TryGetValue(RawBodyStringKey, out var value) && value is string bodyString)
        {
            return bodyString;
        }
        
        return null;
    }
}