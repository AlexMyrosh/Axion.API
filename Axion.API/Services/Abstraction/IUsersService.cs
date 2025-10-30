namespace Axion.API.Services.Abstraction;

public interface IUsersService
{
    Task<List<Dictionary<string, object?>>?> GetActiveUsersAsync(string status);
    Task<List<Dictionary<string, object?>>?> CreateUserAsync(string username, string email, string status);
}