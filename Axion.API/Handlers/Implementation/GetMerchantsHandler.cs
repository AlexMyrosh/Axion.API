using Axion.API.DbRepositories.Abstraction;
using Axion.API.Handlers.Abstraction;
using Axion.API.Models;

namespace Axion.API.Handlers.Implementation;

public class GetMerchantsHandler(IPostgresRepository postgresRepository) : IApiHandler
{
    public async Task<ApiResponse> HandleAsync(ApiRequest request)
    {
        var result = await postgresRepository.DbExecuteAsync(null, "GetMerchants");
        return result is not null ? ApiResponse.Success(result) : ApiResponse.Error("500", "Error getting merchants");
    }
}