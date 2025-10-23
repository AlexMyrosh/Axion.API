using Axion.API.Services.Abstraction;

namespace Axion.API.Services.Implementation;

public class KafkaProducerStub(ILogger<KafkaProducerStub> logger) : IKafkaProducer
{
    public Task ProduceAsync(string topic, string message)
    {
        logger.LogInformation("Kafka message produced to topic {Topic}: {Message}", topic, message);
        return Task.CompletedTask;
    }
}