namespace csharp_template.HealthCheckers.Abstraction;

public interface IDatabaseHealthCheck
{
    Task<bool> CheckHealthAsync();
}