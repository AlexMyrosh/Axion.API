using Axion.API.Models;

namespace Axion.API.Handlers.Abstraction;

public interface IApiHandler
{
    Task<ApiResponse> HandleAsync(ApiRequest request);
}