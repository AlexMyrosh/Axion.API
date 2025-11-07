using csharp_template.DbRepositories.Abstraction;
using csharp_template.Handlers.Abstraction;
using csharp_template.Models;

namespace csharp_template.Handlers.Implementation;

public class GetMerchantsHandler(IPostgresRepository postgresRepository) : IApiHandler
{
    public async Task<ApiResponse> HandleAsync(ApiRequest request)
    {
        var result = await postgresRepository.DbExecuteAsync(null, "GetMerchants");
        return result is not null ? ApiResponse.Success(result) : ApiResponse.Error("500", "Error getting merchants");
    }
}