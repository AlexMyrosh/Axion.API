using System.Diagnostics;
using System.Text.Json;

namespace Axion.API.Middleware;

public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Read body request
        context.Request.EnableBuffering();
        var requestBody = "";
        if (context.Request.ContentLength > 0 && context.Request.ContentType?.Contains("application/json") == true)
        {
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        logger.LogInformation("REQUEST {RequestMethod} {RequestPath} Body: {RequestBody}", context.Request.Method, context.Request.Path, FilterSensitiveData(requestBody));

        // Intercepting response
        var originalBody = context.Response.Body;
        using var memStream = new MemoryStream();
        context.Response.Body = memStream;

        await next(context);

        memStream.Position = 0;
        var responseBody = await new StreamReader(memStream).ReadToEndAsync();
        memStream.Position = 0;

        logger.LogInformation("RESPONSE Code: {StatusCode} Body: {ResponseBody}", context.Response.StatusCode, FilterSensitiveData(responseBody));

        await memStream.CopyToAsync(originalBody);
    }

    private static readonly string[] SensitiveFields = 
    {
        "password", "token", "card_number", "cvv", "cvc", "pin", "secret", "key", 
        "cardnumber", "card", "authorization"
    };

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
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            return JsonSerializer.Serialize(filtered, options);
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
            var propertyNameLower = prop.Name.ToLower();
            if (SensitiveFields.Any(field => propertyNameLower.Contains(field)))
            {
                result[prop.Name] = "***";
            }
            else if (prop.Value.ValueKind == JsonValueKind.Object)
            {
                result[prop.Name] = MaskSensitive(prop.Value);
            }
            else if (prop.Value.ValueKind == JsonValueKind.Array)
            {
                var arrayItems = new List<object>();
                foreach (var item in prop.Value.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object)
                    {
                        arrayItems.Add(MaskSensitive(item));
                    }
                    else
                    {
                        arrayItems.Add(item.ToString());
                    }
                }
                result[prop.Name] = arrayItems;
            }
            else
            {
                result[prop.Name] = prop.Value.ToString();
            }
        }

        return result;
    }
}