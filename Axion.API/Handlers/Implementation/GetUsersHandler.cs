using Axion.API.DbRepositories.Abstraction;
using Axion.API.Handlers.Abstraction;
using Axion.API.Models;

namespace Axion.API.Handlers.Implementation;

public class GetUsersHandler(IPostgresRepository postgresRepository) : IApiHandler
{
    public async Task<ApiResponse> HandleAsync(ApiRequest request)
    {
        var status = request.Query.TryGetValue("status", out var s) && !string.IsNullOrWhiteSpace(s) ? s : "active";
        var parameters = new Dictionary<string, object> 
        {
            ["status"] = status
        };
        var result = await postgresRepository.DbExecuteAsync(null, "GetActiveUsers", parameters);
        return result is not null ? ApiResponse.Success(result) : ApiResponse.Error("500", "Error getting users");
    }
}