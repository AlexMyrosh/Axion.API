using Axion.API.DbRepositories.Abstraction;
using Axion.API.Services.Abstraction;

namespace Axion.API.Services.Implementation;

public class UsersService(IPostgresRepository postgresRepository) : IUsersService
{
    public Task<List<Dictionary<string, object?>>?> GetActiveUsersAsync(string status)
    {
        var parameters = new Dictionary<string, object>
        {
            ["status"] = status
        };
        
        return postgresRepository.DbExecuteAsync("users", "GetActiveUsers", parameters);
    }

    public Task<List<Dictionary<string, object?>>?> CreateUserAsync(string username, string email, string status)
    {
        var parameters = new Dictionary<string, object>
        {
            ["username"] = username,
            ["email"] = email,
            ["status"] = status
        };
        return postgresRepository.DbExecuteAsync("users", "CreateUser", parameters);
    }
}