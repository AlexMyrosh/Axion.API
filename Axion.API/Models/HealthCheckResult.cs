namespace Axion.API.Models;

public class HealthCheckResult
{
    public bool ApiConfigurator { get; set; }
    
    public bool Postgres { get; set; }
    
    public bool Redis { get; set; }
    
    public bool Oracle { get; set; }
    
    public bool Kafka { get; set; }
}