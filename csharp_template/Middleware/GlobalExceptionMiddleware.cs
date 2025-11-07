using System.Net;
using System.Text.Json;
using csharp_template.Models;

namespace csharp_template.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);
        
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = ApiResponse.Error(
            "500", 
            "Something went wrong. Please try again later.", 
            new { payment_status = "error" },
            "processing"
        );

        var jsonResponse = JsonSerializer.Serialize(response.Data);
        await context.Response.WriteAsync(jsonResponse);
    }
}

