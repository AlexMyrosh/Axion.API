using Axion.API.Auth;
using Axion.API.Config;
using Axion.API.Registry;
using Axion.API.Helpers;
using Axion.API.Models;

namespace Axion.API.Middleware;

public class AuthMiddleware(RequestDelegate next)
{
    private const string StaticTokenPrefix = "Static ";
    private const string JwtTokenPrefix = "Bearer ";
    
    public async Task InvokeAsync(HttpContext context, IServiceProvider services, ILogger<AuthMiddleware> logger)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        var method = context.Request.Method.ToUpperInvariant();
        var key = RouteKeyHelper.BuildRouteKey(path, method);

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
                    var authHeader = context.Request.Headers["Authorization"].ToString();
                    if (string.IsNullOrEmpty(authHeader) || authHeader.Length <= JwtTokenPrefix.Length || 
                        !authHeader.StartsWith(JwtTokenPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        logger.LogWarning("Missing or invalid JWT token format");
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        var jwtErrorResponse = ApiResponse.Error("401", "Missing JWT token", new { payment_status = "error" });
                        await context.Response.WriteAsJsonAsync(jwtErrorResponse.Data);
                        return;
                    }

                    var token = authHeader.Substring(JwtTokenPrefix.Length);
                    var jwtProvider = services.GetRequiredService<IAuthProvider>();
                    if (!jwtProvider.Validate(token))
                    {
                        logger.LogWarning("Invalid JWT token");
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
                    var staticProvider = services.GetRequiredService<StaticTokenAuthProvider>();
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