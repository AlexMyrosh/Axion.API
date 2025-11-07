using csharp_template.DbRepositories.Abstraction;
using csharp_template.Handlers.Abstraction;
using csharp_template.Models;
using csharp_template.Utilities;

namespace csharp_template.Handlers.Implementation;

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