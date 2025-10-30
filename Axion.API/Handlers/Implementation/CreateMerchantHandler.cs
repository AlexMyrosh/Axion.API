using System.Text.Json;
using Axion.API.Handlers.Abstraction;
using Axion.API.Models;
using Axion.API.Services.Abstraction;

namespace Axion.API.Handlers.Implementation;

public class CreateMerchantHandler(IMerchantsService merchantsService, ILogger<CreateMerchantHandler> logger) : IApiHandler
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

            var result = await merchantsService.CreateMerchantAsync(name, email);
            return ApiResponse.Success(result ?? []);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating merchant");
            return ApiResponse.Error("500", "Create merchant failed");
        }
    }
}