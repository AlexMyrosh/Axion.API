namespace Axion.API.Services.Abstraction;

// Redis
public interface IRedisService
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
}