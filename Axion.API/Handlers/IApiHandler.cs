using Axion.API.Models;

namespace Axion.API.Handlers;

public interface IApiHandler
{
    Task<ApiResponse> HandleAsync(ApiRequest request);
}