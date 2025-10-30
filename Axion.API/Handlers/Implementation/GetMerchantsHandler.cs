using Axion.API.Handlers.Abstraction;
using Axion.API.Models;
using Axion.API.Services.Abstraction;

namespace Axion.API.Handlers.Implementation;

public class GetMerchantsHandler(IMerchantsService merchantsService) : IApiHandler
{
    public async Task<ApiResponse> HandleAsync(ApiRequest request)
    {
        var result = await merchantsService.GetMerchantsAsync();
        return result is not null ? ApiResponse.Success(result) : ApiResponse.Error("500", "Error getting merchants");
    }
}