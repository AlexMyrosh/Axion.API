using Axion.API.Services.Abstraction;

namespace Axion.API.Services.Implementation;

public class RedisServiceStub : IRedisService
{
    private readonly Dictionary<string, string> _cache = new();

    public Task<string?> GetAsync(string key)
    {
        _cache.TryGetValue(key, out var value);
        return Task.FromResult(value);
    }

    public Task SetAsync(string key, string value)
    {
        _cache[key] = value;
        return Task.CompletedTask;
    }
}