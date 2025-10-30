using Axion.API.DbRepositories.Abstraction;
using Axion.API.HealthCheckers.Abstraction;

namespace Axion.API.HealthCheckers.Implementation;

public class PostgresHealthCheck(IPostgresRepository postgresService, ILogger<PostgresHealthCheck> logger) : IPostgresHealthCheck
{
    public async Task<bool> CheckHealthAsync()
    {
        if (!postgresService.IsInit || postgresService.IsProgress)
        {
            logger.LogWarning("PostgreSQL health check failed (state): IsInit={IsInit}, IsInProgress={IsProgress}", postgresService.IsInit, postgresService.IsProgress);
            return false;
        }
    
        logger.LogInformation("PostgreSQL health check passed");
        return true;
    }
}