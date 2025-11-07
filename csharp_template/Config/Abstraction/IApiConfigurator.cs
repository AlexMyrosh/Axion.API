using csharp_template.Models;

namespace csharp_template.Config.Abstraction;

public interface IApiConfigurator
{
    bool IsReady { get; }

    public Task ConfigureAsync();

    public string? GetAuthTypeForHandler(Type handlerType);

    public RequestSchema? GetRequestSchemaForRoute(string routeKey);
}