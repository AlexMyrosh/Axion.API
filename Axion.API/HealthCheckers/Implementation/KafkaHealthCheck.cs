using Axion.API.HealthCheckers.Abstraction;
using Confluent.Kafka;

namespace Axion.API.HealthCheckers.Implementation;

public class KafkaHealthCheck(IConfiguration configuration, ILogger<KafkaHealthCheck> logger) : IKafkaHealthCheck
{
    public Task<bool> CheckHealthAsync()
    {
        try
        {
            var bootstrapServers = configuration["Kafka:BootstrapServers"];
            var config = new AdminClientConfig
            {
                BootstrapServers = bootstrapServers
            };

            using var client = new AdminClientBuilder(config).Build();
            
            var metadata = client.GetMetadata(TimeSpan.FromSeconds(5));
            
            logger.LogInformation("Kafka health check passed");
            return Task.FromResult(metadata != null && metadata.Brokers.Count != 0);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Kafka health check failed");
            return Task.FromResult(false);
        }
    }
}
