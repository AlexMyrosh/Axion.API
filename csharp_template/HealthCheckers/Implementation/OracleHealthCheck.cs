using csharp_template.HealthCheckers.Abstraction;
using Oracle.ManagedDataAccess.Client;

namespace csharp_template.HealthCheckers.Implementation;

public class OracleHealthCheck(IConfiguration configuration, ILogger<OracleHealthCheck> logger) : IOracleHealthCheck
{
    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            var connectionString = configuration.GetConnectionString("Oracle");
            await using var connection = new OracleConnection(connectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1 FROM DUAL";
            await command.ExecuteScalarAsync();
            
            logger.LogInformation("Oracle health check passed");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Oracle health check failed");
            return false;
        }
    }
}