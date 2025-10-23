using System.Text.Json;
using Axion.API.Config;
using Axion.API.Helpers;
using Axion.API.Models;
using Axion.API.Validation;

namespace Axion.API.Middleware;

public class ValidationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ApiConfigurator apiConfigurator, RequestValidator validator, ILogger<ValidationMiddleware> logger)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method.ToUpperInvariant();
        var key = RouteKeyHelper.BuildRouteKey(path, method);

        var schema = apiConfigurator.GetRequestSchemaForRoute(key);
        
        // If no schema defined or no fields, skip validation
        if (schema == null || schema.Fields.Count == 0)
        {
            await next(context);
            return;
        }

        // Only validate if there's a body to validate (for POST, PUT, PATCH)
        if (context.Request.ContentLength > 0 && context.Request.ContentType?.Contains("application/json") == true)
        {
            // Enable buffering so we can read the body multiple times
            context.Request.EnableBuffering();
            
            JsonElement? body = null;
            
            try
            {
                using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                var bodyString = await reader.ReadToEndAsync();
                
                // Reset the stream position for the next middleware/controller
                context.Request.Body.Position = 0;

                if (!string.IsNullOrWhiteSpace(bodyString))
                {
                    var jsonDoc = JsonDocument.Parse(bodyString);
                    body = jsonDoc.RootElement;
                    
                    // Store the parsed body in HttpContext.Items for reuse in controller
                    context.Items["ParsedRequestBody"] = jsonDoc;
                }
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Invalid JSON in request body");
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                var errorResponse = ApiResponse.Error("400", "Request body contains invalid JSON", new { payment_status = "error" });
                await context.Response.WriteAsJsonAsync(errorResponse.Data);
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error reading request body");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                var errorResponse = ApiResponse.Error("500", "Something went wrong. Please try again later.", new { payment_status = "error" });
                await context.Response.WriteAsJsonAsync(errorResponse.Data);
                return;
            }

            var errors = validator.Validate(body, schema);
            
            if (errors.Count > 0)
            {
                logger.LogWarning("Validation failed for {Method} {Path}: {ErrorCount} errors", method, path, errors.Count);
                
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
                
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                var errorResponse = ApiResponse.Error("400", "Request validation failed", new { payment_status = "error" });
                await context.Response.WriteAsJsonAsync(errorResponse.Data);
                return;
            }
        }

        await next(context);
    }
}

