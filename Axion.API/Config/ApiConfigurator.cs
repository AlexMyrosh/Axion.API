using Axion.API.Config.Abstraction;
using Axion.API.Utilities;
using Axion.API.Models;
using Axion.API.Registry;
using Axion.API.Validation;

namespace Axion.API.Config;

public class ApiConfigurator(
    IConfiguration configuration, 
    HandlerRegistry registry, 
    ApiRouteValidator validator,
    ILogger<ApiConfigurator> logger) : IApiConfigurator
{
    private readonly Dictionary<Type, string> _authMap = new();
    private readonly Dictionary<string, RequestSchema?> _routeSchemaMap = new();
    public bool IsReady { get; private set; }

    public Task ConfigureAsync()
    {
        IsReady = false;
        var container = new ApiRoutesContainer();
        configuration.Bind(container);

        if (container.ApiRoutes.Count == 0)
        {
            logger.LogWarning("No API routes found in configuration");
            IsReady = true;
            return Task.CompletedTask;
        }

        var totalRoutes = container.ApiRoutes.Count;
        var validRoutes = 0;
        var invalidRoutes = 0;

        for (var i = 0; i < container.ApiRoutes.Count; i++)
        {
            var route = container.ApiRoutes[i];
            
            // Validate route structure
            if (!validator.ValidateRoute(route, i, out var validationErrors))
            {
                invalidRoutes++;
                logger.LogError("Route validation failed. Skipping route. Total errors: {ErrorCount}", validationErrors.Count);
                continue;
            }
            
            var handlerType = Type.GetType(route.Handler)!;
            var key = RouteKeyUtility.BuildRouteKey(route.Path, route.Method);
            
            // Check for duplicate routes
            if (registry.TryGet(key, out _))
            {
                invalidRoutes++;
                logger.LogError("Duplicated route detected: '{Method} {Path}'. Skipping the route.", route.Method, route.Path);
                continue;
            }
            
            registry.Register(key, handlerType);
            _authMap[handlerType] = route.Auth.ToLowerInvariant();
            _routeSchemaMap[key] = route.RequestSchema;
            
            validRoutes++;
            logger.LogInformation("Route registered: {Method} {Path} -> {Handler} (Auth: {Auth})", route.Method, route.Path, handlerType.Name, route.Auth);
        }

        if (invalidRoutes == 0)
        {
            logger.LogInformation("API Routes validation completed successfully. Total: {Total}, Valid: {Valid}", totalRoutes, validRoutes);
        }
        else
        {
            logger.LogWarning("API Routes validation completed with errors. Total: {Total}, Valid: {Valid}, Invalid: {Invalid}", totalRoutes, validRoutes, invalidRoutes);
        }

        IsReady = validRoutes > 0;
        if (IsReady)
        {
            logger.LogInformation("ApiConfigurator is ready");
        }
        else
        {
            logger.LogError("ApiConfigurator failed to initialize - no valid routes found");
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