using csharp_template.DbRepositories.Abstraction;
using csharp_template.Handlers.Abstraction;
using csharp_template.Models;
using csharp_template.Utilities;

namespace csharp_template.Handlers.Implementation;

public class CreateMerchantHandler(IPostgresRepository postgresRepository, ILogger<CreateMerchantHandler> logger) : IApiHandler
{
    public async Task<ApiResponse> HandleAsync(ApiRequest request)
    {
        try
        {
            var name = RequestDataExtractor.GetValue("name", request.Parsed);
            var email = RequestDataExtractor.GetValue("email", request.Parsed);
            if (name is null || email is null)
            {
                return ApiResponse.Error("500", "Create merchant failed");
            }
            
            var parameters = new Dictionary<string, object> 
            {
                ["name"] = name, 
                ["email"] = email 
            };

            var result = await postgresRepository.DbExecuteAsync(null, "CreateMerchant", parameters);
            return result is not null ? ApiResponse.Success(result) : ApiResponse.Error("500", "Create merchant failed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating merchant");
            return ApiResponse.Error("500", "Create merchant failed");
        }
    }
}