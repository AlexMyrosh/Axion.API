using Axion.API.DbRepositories.Abstraction;
using Axion.API.Handlers.Abstraction;
using Axion.API.Models;
using Axion.API.Utilities;

namespace Axion.API.Handlers.Implementation;

public class CreateUserHandler(IPostgresRepository postgresRepository, ILogger<CreateUserHandler> logger) : IApiHandler
{
    public async Task<ApiResponse> HandleAsync(ApiRequest request)
    {
        try
        {
            var username = RequestDataExtractor.GetValue("username", request.Parsed);
            var email = RequestDataExtractor.GetValue("email", request.Parsed);
            var status = RequestDataExtractor.GetValue("status", request.Parsed, defaultValue: "active");
            if (username is null || email is null)
            {
                return ApiResponse.Error("500", "Create user failed");
            }
            
            var parameters = new Dictionary<string, object>
             {
                 ["username"] = username,
                 ["email"] = email,
                 ["status"] = status!
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