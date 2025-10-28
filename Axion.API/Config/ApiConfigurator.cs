using Axion.API.Utilities;
using Axion.API.Models;
using Axion.API.Registry;

namespace Axion.API.Config;

public class ApiConfigurator(IConfiguration configuration, HandlerRegistry registry, ILogger<ApiConfigurator> logger)
{
    private readonly Dictionary<Type, string> _authMap = new();
    private readonly Dictionary<string, RequestSchema?> _routeSchemaMap = new();

    public Task ConfigureAsync()
    {
        var container = new ApiRoutesContainer();
        configuration.Bind(container);

        foreach (var route in container.ApiRoutes)
        {
            var handlerType = Type.GetType(route.Handler);
            if (handlerType == null)
            {
                logger.LogWarning("Handler type not found: {Handler}", route.Handler);
                continue;
            }
            
            var key = RouteKeyUtility.BuildRouteKey(route.Path, route.Method);
            registry.Register(key, handlerType);
            
            _authMap[handlerType] = route.Auth.ToLowerInvariant();
            _routeSchemaMap[key] = route.RequestSchema;
            
            logger.LogInformation("Route registered: {Method} {Path} -> {Handler} (Auth: {Auth})", 
                route.Method, route.Path, handlerType.Name, route.Auth);
        }

        return Task.CompletedTask;
    }
    
    public string? GetAuthTypeForHandler(Type handlerType)
    {
        _authMap.TryGetValue(handlerType, out var authType);
        return authType;
    }
    
    public RequestSchema? GetRequestSchemaForRoute(string routeKey)
    {
        _routeSchemaMap.TryGetValue(routeKey, out var schema);
        return schema;
    }
}