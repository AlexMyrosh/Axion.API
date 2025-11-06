using System.Text.Json;

namespace Axion.API.Middleware;

public class RequestDataReadingMiddleware(RequestDelegate next, ILogger<RequestDataReadingMiddleware> logger)
{
    private const string ParsedBodyJsonElementKey = "ParsedBodyJsonElement";
    private const string RawBodyStringKey = "RawBodyString";
    private const string ParsedRequestDataJsonElementKey = "ParsedRequestDataJsonElement";

    public async Task InvokeAsync(HttpContext context)
    {
        var combinedData = new Dictionary<string, object?>();
        
        // Process Query parameters
        if (context.Request.Query.Count != 0)
        {
            foreach (var (key, value) in context.Request.Query)
            {
                combinedData[key] = value.ToString();
            }
        }
        
        // Process Body
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
                        
                        // Add Body properties to combinedData (Body overwrites Query if same key)
                        foreach (var property in jsonElement.EnumerateObject())
                        {
                            combinedData[property.Name] = property.Value;
                        }
                        
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
        
        // Create combined JsonElement from dictionary
        if (combinedData.Count > 0)
        {
            try
            {
                var jsonString = JsonSerializer.Serialize(combinedData);
                var combinedDoc = JsonDocument.Parse(jsonString);
                var combinedElement = combinedDoc.RootElement.Clone();
                context.Items[ParsedRequestDataJsonElementKey] = combinedElement;
                
                logger.LogDebug("Combined request data (Query + Body) parsed and cached. Total keys: {Count}", combinedData.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating combined request data");
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
    
    public static JsonElement? GetParsedRequestData(HttpContext context)
    {
        if (context.Items.TryGetValue(ParsedRequestDataJsonElementKey, out var value) && value is JsonElement jsonElement)
        {
            return jsonElement;
        }
        
        return null;
    }
}