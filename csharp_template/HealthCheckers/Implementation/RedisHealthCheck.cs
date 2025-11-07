using csharp_template.HealthCheckers.Abstraction;
using StackExchange.Redis;

namespace csharp_template.HealthCheckers.Implementation;

public class RedisHealthCheck(IConfiguration configuration, ILogger<RedisHealthCheck> logger) : IRedisHealthCheck
{
    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            var connectionString = configuration.GetConnectionString("Redis");
            var connection = await ConnectionMultiplexer.ConnectAsync(connectionString!);
            var result = await connection.GetDatabase().PingAsync();
            
            logger.LogInformation("Redis health check passed in {ElapsedMilliseconds}ms", result.TotalMilliseconds);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis health check failed");
            return false;
        }
    }
}