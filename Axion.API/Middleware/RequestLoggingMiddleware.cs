using System.Diagnostics;
using System.Text.Json;

namespace Axion.API.Middleware;

public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var processId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        context.Items["ProcessId"] = processId;
        context.Response.Headers["X-Process-Id"] = processId;

        // Read body request
        context.Request.EnableBuffering();
        var requestBody = "";
        if (context.Request.ContentLength > 0 && context.Request.ContentType?.Contains("application/json") == true)
        {
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        logger.LogInformation("REQUEST {RequestMethod} {RequestPath} [{ProcessId}] => Body: {FilterSensitiveData}", 
            context.Request.Method, context.Request.Path, processId, FilterSensitiveData(requestBody));

        // Intercepting response
        var originalBody = context.Response.Body;
        using var memStream = new MemoryStream();
        context.Response.Body = memStream;

        await next(context);

        memStream.Position = 0;
        var responseBody = await new StreamReader(memStream).ReadToEndAsync();
        memStream.Position = 0;

        logger.LogInformation("RESPONSE [{ProcessId}] => Code: {ResponseStatusCode}, Body: {FilterSensitiveData}", 
            processId, context.Response.StatusCode, FilterSensitiveData(responseBody));

        await memStream.CopyToAsync(originalBody);
    }

    private static string FilterSensitiveData(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }
        
        try
        {
            using var doc = JsonDocument.Parse(input);
            var filtered = MaskSensitive(doc.RootElement);
            return JsonSerializer.Serialize(filtered);
        }
        catch
        {
            return input;
        }
    }

    private static Dictionary<string, object> MaskSensitive(JsonElement element)
    {
        var result = new Dictionary<string, object>();
        foreach (var prop in element.EnumerateObject())
        {
            if (prop.Name.ToLower().Contains("password") || prop.Name.ToLower().Contains("token"))
            {
                result[prop.Name] = "***";
            }
            else if (prop.Value.ValueKind == JsonValueKind.Object)
            {
                result[prop.Name] = MaskSensitive(prop.Value);
            }
            else
            {
                result[prop.Name] = prop.Value.ToString();
            }
        }

        return result;
    }
}