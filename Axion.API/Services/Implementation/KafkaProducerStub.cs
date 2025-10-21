using Axion.API.Services.Abstraction;

namespace Axion.API.Services.Implementation;

public class KafkaProducerStub : IKafkaProducer
{
    public Task ProduceAsync(string topic, string message)
    {
        Console.WriteLine($"[KafkaProducerStub] Topic={topic}, Message={message}");
        return Task.CompletedTask;
    }
}