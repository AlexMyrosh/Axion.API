using Axion.API.Utilities;
using Serilog.Context;

namespace Axion.API.Middleware;

public class ProcessIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var processId = ProcessIdGenerator.Generate();
        using (LogContext.PushProperty("ProcessId", processId))
        {
            await next(context);
        }
    }
}