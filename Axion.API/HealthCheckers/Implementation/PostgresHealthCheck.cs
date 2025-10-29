using Axion.API.HealthCheckers.Abstraction;
using Npgsql;

namespace Axion.API.HealthCheckers.Implementation;

public class PostgresHealthCheck(IConfiguration configuration, ILogger<PostgresHealthCheck> logger) : IPostgresHealthCheck
{
    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            var connectionString = configuration.GetConnectionString("Postgres");
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync();
            
            logger.LogInformation("PostgreSQL health check passed");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "PostgreSQL health check failed");
            return false;
        }
    }
}