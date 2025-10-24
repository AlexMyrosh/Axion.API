using System.Text.Json;
using Axion.API.Handlers;
using Axion.API.Helpers;
using Axion.API.Models;
using Axion.API.Registry;
using Microsoft.AspNetCore.Mvc;

namespace Axion.API.Controllers;

[ApiController]
[Route("api/{**slug}")]
public class DynamicController(HandlerRegistry registry, IServiceProvider services) : ControllerBase
{
    [HttpPost]
    [HttpGet]
    [HttpPut]
    [HttpDelete]
    public async Task<IActionResult> Handle()
    {
        var path = Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        var method = Request.Method.ToUpperInvariant();
        var key = RouteKeyHelper.BuildRouteKey(Request.Path.Value, Request.Method);
        
        if (!registry.TryGet(key, out var handlerType) || handlerType == null)
        {
            return NotFound(ApiResponse.Error("404", "Handler not found", new { payment_status = "error" }));
        }

        var request = new ApiRequest
        {
            Path = path, 
            Method = method
        };
        
        // Headers
        foreach (var h in Request.Headers)
        {
            request.Headers[h.Key] = h.Value.ToString();
        }

        foreach (var q in Request.Query)
        {
            request.Query[q.Key] = q.Value.ToString();
        }
        
        using var sr = new StreamReader(Request.Body);
        var bodyStr = await sr.ReadToEndAsync();
        if (!string.IsNullOrWhiteSpace(bodyStr))
        {
            request.Body = JsonDocument.Parse(bodyStr).RootElement;
        }

        var handler = (IApiHandler?)services.GetService(handlerType) ?? ActivatorUtilities.CreateInstance(services, handlerType) as IApiHandler;
        if (handler == null)
        {
            return StatusCode(500, ApiResponse.Error("500", "Handler creation failed", new { payment_status = "error" }));
        }

        var response = await handler.HandleAsync(request);
        return StatusCode(response.StatusCode, response.Data);
    }
}