using Axion.API.Auth;
using Axion.API.Config;
using Axion.API.Registry;

namespace Axion.API.Middleware;

public class AuthMiddleware(RequestDelegate next)
{
    private const string StaticTokenPrefix = "Static ";
    private const string JwtTokenPrefix = "Bearer ";
    
    public async Task InvokeAsync(HttpContext context, IServiceProvider services, ILogger<AuthMiddleware> logger)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        var method = context.Request.Method.ToUpperInvariant();
        var key = path + ":" + method;

        var registry = services.GetRequiredService<HandlerRegistry>();
        if (!registry.TryGet(key, out var handlerType) || handlerType == null)
        {
            logger.LogInformation("No handler found for {Method} {Path}", method, path);
            await next(context);
            return;
        }
        
        var apiConfigurator = services.GetRequiredService<ApiConfigurator>();
        var authType = apiConfigurator.GetAuthTypeForHandler(handlerType);
        
        logger.LogInformation("Authenticating request {Method} {Path} with AuthType {AuthType}", method, path, authType);
        
        switch (authType?.ToLowerInvariant())
        {
            case "jwt":
                {
                    var authHeader = context.Request.Headers["Authorization"].ToString();
                    if (!authHeader.StartsWith(JwtTokenPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        logger.LogWarning("Missing JWT token for {Method} {Path}", method, path);
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(new { error = "missing_jwt_token" });
                        return;
                    }

                    var jwtProvider = services.GetRequiredService<IAuthProvider>();
                    if (!jwtProvider.Validate(authHeader.Substring(JwtTokenPrefix.Length)))
                    {
                        logger.LogWarning("Invalid JWT token for {Method} {Path}", method, path);
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(new { error = "invalid_jwt_token" });
                        return;
                    }
                    
                    logger.LogInformation("JWT token validated successfully for {Method} {Path}", method, path);
                }
                break;

            case "static_tokens":
                {
                    var authHeader = context.Request.Headers["Authorization"].ToString();
                    if (!authHeader.StartsWith(StaticTokenPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        logger.LogWarning("Missing static token for {Method} {Path}", method, path);
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(new { error = "missing_static_token" });
                        return;
                    }

                    var staticProvider = services.GetRequiredService<StaticTokenAuthProvider>();
                    if (!staticProvider.Validate(authHeader.Substring(StaticTokenPrefix.Length)))
                    {
                        logger.LogWarning("Invalid static token for {Method} {Path}", method, path);
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(new { error = "invalid_static_token" });
                        return;
                    }
                    
                    logger.LogInformation("Static token validated successfully for {Method} {Path}", method, path);
                }
                break;

            case "none":
                logger.LogInformation("No authentication required for {Method} {Path}", method, path);
                break;
            
            default:
                logger.LogError("Unsupported AuthType {AuthType} for {Method} {Path}", authType, method, path);
                context.Response.StatusCode = StatusCodes.Status501NotImplemented;
                await context.Response.WriteAsJsonAsync(new { error = "selected_auth_not_supported" });
                return;
        }

        await next(context);
    }
}