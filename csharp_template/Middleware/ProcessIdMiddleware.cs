using csharp_template.Utilities;
using Serilog.Context;

namespace csharp_template.Middleware;

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