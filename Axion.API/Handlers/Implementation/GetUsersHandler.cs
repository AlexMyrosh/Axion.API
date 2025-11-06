using Axion.API.DbRepositories.Abstraction;
using Axion.API.Handlers.Abstraction;
using Axion.API.Models;
using Axion.API.Utilities;

namespace Axion.API.Handlers.Implementation;

public class GetUsersHandler(IPostgresRepository postgresRepository) : IApiHandler
{
    public async Task<ApiResponse> HandleAsync(ApiRequest request)
    {
        var status = RequestDataExtractor.GetValue("status", request.Parsed, defaultValue: "active");
        var parameters = new Dictionary<string, object> 
        {
            ["status"] = status!
        };
        var result = await postgresRepository.DbExecuteAsync(null, "GetActiveUsers", parameters);
        return result is not null ? ApiResponse.Success(result) : ApiResponse.Error("500", "Error getting users");
    }
}