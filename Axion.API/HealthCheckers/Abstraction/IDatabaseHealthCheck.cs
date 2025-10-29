namespace Axion.API.HealthCheckers.Abstraction;

public interface IDatabaseHealthCheck
{
    Task<bool> CheckHealthAsync();
}