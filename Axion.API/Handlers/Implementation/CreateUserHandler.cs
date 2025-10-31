using Axion.API.DbRepositories.Abstraction;
using Axion.API.Handlers.Abstraction;
using Axion.API.Models;

namespace Axion.API.Handlers.Implementation;

public class CreateUserHandler(IPostgresRepository postgresRepository, ILogger<CreateUserHandler> logger) : IApiHandler
{
    public async Task<ApiResponse> HandleAsync(ApiRequest request)
    {
        try
        {
            var username = string.Empty;
            var email = string.Empty;
            var status = "active";
            if (request.Body.HasValue)
            {
                if (request.Body.Value.TryGetProperty("username", out var usernameProp))
                {
                    username = usernameProp.GetString() ?? string.Empty;
                }
                if (request.Body.Value.TryGetProperty("email", out var emailProp))
                {
                    email = emailProp.GetString() ?? string.Empty;
                }
                if (request.Body.Value.TryGetProperty("status", out var statusProp))
                {
                    status = statusProp.GetString() ?? status;
                }
            }
            
            var parameters = new Dictionary<string, object>
             {
                 ["username"] = username,
                 ["email"] = email,
                 ["status"] = status
             };
            var result = await postgresRepository.DbExecuteAsync(null, "CreateUser", parameters);
            return result is not null ? ApiResponse.Success(result) : ApiResponse.Error("500", "Create user failed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating user");
            return ApiResponse.Error("500", "Create user failed");
        }
    }
}