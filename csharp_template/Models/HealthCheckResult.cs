namespace csharp_template.Models;

public class HealthCheckResult
{
    public bool ApiConfigurator { get; init; }
    
    public bool QueryConfigurator { get; init; }
    
    public bool Postgres { get; init; }
    
    // public bool Redis { get; set; }
    //
    // public bool Oracle { get; set; }
    //
    // public bool Kafka { get; set; }
}