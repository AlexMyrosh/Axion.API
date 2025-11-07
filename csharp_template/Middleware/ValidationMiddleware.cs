using csharp_template.Config.Abstraction;
using csharp_template.Models;
using csharp_template.Utilities;
using csharp_template.Validation;

namespace csharp_template.Middleware;

public class ValidationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IApiConfigurator apiConfigurator, RequestValidator validator, ILogger<ValidationMiddleware> logger)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        
        // Skip validation for health check endpoint
        if (path.Contains("/api/health_check", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }
        
        var method = context.Request.Method.ToUpperInvariant();
        var key = RouteKeyUtility.BuildRouteKey(path, method);

        var schema = apiConfigurator.GetRequestSchemaForRoute(key);
        
        // If no schema defined or no fields, skip validation
        if (schema?.Fields == null || schema.Fields.Count == 0)
        {
            await next(context);
            return;
        }
        
        var body = RequestDataReadingMiddleware.GetJsonBody(context);
        
        // Check if body was expected but failed to parse
        if (context.Request.ContentLength > 0 && 
            context.Request.ContentType?.Contains("application/json") == true && 
            body == null &&
            RequestDataReadingMiddleware.GetRawBody(context) != null)
        {
            logger.LogWarning("Invalid JSON in request body");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var errorResponse = ApiResponse.Error("400", "Request body contains invalid JSON", new { payment_status = "error" });
            await context.Response.WriteAsJsonAsync(errorResponse.Data);
            return;
        }
        
        if (body != null)
        {
            var errors = validator.Validate(body, schema);
            
            if (errors.Count > 0)
            {
                logger.LogWarning("Validation failed for {Method} {Path}: {ErrorCount} errors", method, path, errors.Count);
                logger.LogWarning("Validation errors: {Errors}", errors.Select(e => e.Message));
                
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                var errorResponse = ApiResponse.Error("400", "Request validation failed", new { payment_status = "error" });
                await context.Response.WriteAsJsonAsync(errorResponse.Data);
                return;
            }
        }
        else if (schema.Fields.Any(f => f.Required))
        {
            // If there's no body  but required fields exist, validate against null
            var errors = validator.Validate(null, schema);
            
            if (errors.Count > 0)
            {
                logger.LogWarning("Validation failed for {Method} {Path}: Missing required fields", method, path);
                logger.LogWarning("Validation errors: {Errors}", errors.Select(e => e.Message));
                
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                var errorResponse = ApiResponse.Error("400", "Request validation failed", new { payment_status = "error" });
                await context.Response.WriteAsJsonAsync(errorResponse.Data);
                return;
            }
        }

        await next(context);
    }
}

