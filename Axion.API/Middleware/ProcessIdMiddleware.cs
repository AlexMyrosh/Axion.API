namespace Axion.API.Middleware;

public class ProcessIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = Guid.NewGuid().ToString("N")[..8]; // Short 8-character ID;
        using (Serilog.Context.LogContext.PushProperty("ProcessId", requestId))
        {
            await next(context);
        }
    }
}