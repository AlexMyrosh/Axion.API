using Axion.API.Services.Abstraction;

namespace Axion.API.Services.Implementation;

public class KafkaConsumerStub(ILogger<KafkaConsumerStub> logger) : IKafkaConsumer
{
    public Task StartAsync()
    {
        logger.LogInformation("Kafka consumer started listening");
        return Task.CompletedTask;
    }
}