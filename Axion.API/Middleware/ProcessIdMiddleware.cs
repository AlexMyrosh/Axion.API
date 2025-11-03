using Serilog.Context;

namespace Axion.API.Middleware;

public class ProcessIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var processId = Guid.NewGuid().ToString("N")[..8];
        using (LogContext.PushProperty("ProcessId", processId))
        {
            await next(context);
        }
    }
}