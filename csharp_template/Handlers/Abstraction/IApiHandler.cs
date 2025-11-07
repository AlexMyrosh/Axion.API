using csharp_template.Models;

namespace csharp_template.Handlers.Abstraction;

public interface IApiHandler
{
    Task<ApiResponse> HandleAsync(ApiRequest request);
}