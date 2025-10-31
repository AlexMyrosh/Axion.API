using Axion.API.DbRepositories.Abstraction;
using Axion.API.Handlers.Abstraction;
using Axion.API.Models;

namespace Axion.API.Handlers.Implementation;

public class CreateMerchantHandler(IPostgresRepository postgresRepository, ILogger<CreateMerchantHandler> logger) : IApiHandler
{
    public async Task<ApiResponse> HandleAsync(ApiRequest request)
    {
        try
        {
            var name = string.Empty;
            var email = string.Empty;
            if (request.Body.HasValue)
            {
                if (request.Body.Value.TryGetProperty("name", out var nameProp))
                {
                    name = nameProp.GetString() ?? string.Empty;
                }
                if (request.Body.Value.TryGetProperty("email", out var emailProp))
                {
                    email = emailProp.GetString() ?? string.Empty;
                }
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