using Axion.API.Config;
using Axion.API.Registry;
using Axion.API.Models;
using System.Text.Json;
using Axion.API.Auth.Abstraction;
using Axion.API.Utilities;

namespace Axion.API.Middleware;

public class AuthMiddleware(RequestDelegate next)
{
    private const string StaticTokenPrefix = "Static ";
    
    public async Task InvokeAsync(HttpContext context, IServiceProvider services, ILogger<AuthMiddleware> logger)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        
        // Skip authentication for health check endpoint
        if (path.Contains("/api/health_check", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }
        
        var method = context.Request.Method.ToUpperInvariant();
        var key = RouteKeyUtility.BuildRouteKey(path, method);

        var registry = services.GetRequiredService<HandlerRegistry>();
        if (!registry.TryGet(key, out var handlerType) || handlerType == null)
        {
            await next(context);
            return;
        }
        
        var apiConfigurator = services.GetRequiredService<ApiConfigurator>();
        var authType = apiConfigurator.GetAuthTypeForHandler(handlerType);
        
        switch (authType?.ToLowerInvariant())
        {
            case "jwt":
                {
                    var jwtToken = context.Request.Headers["x-auth-token"].ToString();
                    if (string.IsNullOrEmpty(jwtToken))
                    {
                        logger.LogWarning("Missing JWT token");
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        var jwtErrorResponse = ApiResponse.Error("401", "Missing JWT token", new { payment_status = "error" });
                        await context.Response.WriteAsJsonAsync(jwtErrorResponse.Data);
                        return;
                    }
                    
                    // Read body to check for timestamp
                    JsonElement? requestBody = null;
                    if (context.Request.ContentLength > 0 && context.Request.ContentType?.Contains("application/json") == true)
                    {
                        context.Request.EnableBuffering();
                        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                        var bodyStr = await reader.ReadToEndAsync();
                        context.Request.Body.Position = 0;
                        
                        if (!string.IsNullOrWhiteSpace(bodyStr))
                        {
                            try
                            {
                                var jsonDoc = JsonDocument.Parse(bodyStr);
                                requestBody = jsonDoc.RootElement;
                            }
                            catch (JsonException ex)
                            {
                                logger.LogWarning(ex, "Failed to parse request body as JSON");
                            }
                        }
                    }
                    
                    var jwtProvider = services.GetRequiredService<IJwtAuthProvider>();
                    if (!jwtProvider.Validate(jwtToken, requestBody))
                    {
                        logger.LogWarning("Invalid JWT token or body validation failed");
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        var jwtInvalidResponse = ApiResponse.Error("401", "Invalid JWT token", new { payment_status = "error" });
                        await context.Response.WriteAsJsonAsync(jwtInvalidResponse.Data);
                        return;
                    }
                }
                break;

            case "static_tokens":
                {
                    var authHeader = context.Request.Headers["Authorization"].ToString();
                    if (string.IsNullOrEmpty(authHeader) || authHeader.Length <= StaticTokenPrefix.Length || 
                        !authHeader.StartsWith(StaticTokenPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        logger.LogWarning("Missing or invalid static token format");
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        var staticErrorResponse = ApiResponse.Error("401", "Missing static token", new { payment_status = "error" });
                        await context.Response.WriteAsJsonAsync(staticErrorResponse.Data);
                        return;
                    }

                    var token = authHeader.Substring(StaticTokenPrefix.Length);
                    var staticProvider = services.GetRequiredService<IStaticTokenAuthProvider>();
                    if (!staticProvider.Validate(token))
                    {
                        logger.LogWarning("Invalid static token");
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        var staticInvalidResponse = ApiResponse.Error("401", "Invalid static token", new { payment_status = "error" });
                        await context.Response.WriteAsJsonAsync(staticInvalidResponse.Data);
                        return;
                    }
                }
                break;

            case "none":
                break;
            
            default:
                logger.LogError("Unsupported auth type: {AuthType}", authType);
                context.Response.StatusCode = StatusCodes.Status501NotImplemented;
                var authErrorResponse = ApiResponse.Error("501", "Selected auth not supported", new { payment_status = "error" });
                await context.Response.WriteAsJsonAsync(authErrorResponse.Data);
                return;
        }

        await next(context);
    }
}