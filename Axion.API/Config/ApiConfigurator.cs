using Axion.API.Models;
using Axion.API.Registry;

namespace Axion.API.Config;

public class ApiConfigurator(IConfiguration configuration, HandlerRegistry registry)
{
    private readonly Dictionary<Type, string> _authMap = new();

    public Task ConfigureAsync()
    {
        var container = new ApiRoutesContainer();
        configuration.Bind(container);

        foreach (var route in container.ApiRoutes)
        {
            var handlerType = Type.GetType(route.Handler);
            if (handlerType == null)
            {
                continue;
            }
            
            var key = route.Path.ToLowerInvariant() + ":" + route.Method.ToUpperInvariant();
            registry.Register(key, handlerType);
            
            _authMap[handlerType] = route.Auth?.ToLowerInvariant() ?? "none";
        }

        return Task.CompletedTask;
    }
    
    public string? GetAuthTypeForHandler(Type handlerType)
    {
        _authMap.TryGetValue(handlerType, out var authType);
        return authType;
    }
}