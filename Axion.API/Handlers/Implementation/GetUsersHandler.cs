using Axion.API.Handlers.Abstraction;
using Axion.API.Models;
using Axion.API.Services.Abstraction;

namespace Axion.API.Handlers.Implementation;

public class GetUsersHandler(IUsersService usersService) : IApiHandler
{
    public async Task<ApiResponse> HandleAsync(ApiRequest request)
    {
        var status = request.Query.TryGetValue("status", out var s) && !string.IsNullOrWhiteSpace(s) ? s : "active";
        var result = await usersService.GetActiveUsersAsync(status);
        return ApiResponse.Success(result ?? []);
    }
}