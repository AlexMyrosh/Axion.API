using Axion.API.Services.Abstraction;

namespace Axion.API.Services.Implementation;

public class KafkaConsumerStub : IKafkaConsumer
{
    public Task StartAsync()
    {
        Console.WriteLine("[KafkaConsumerStub] Listening...");
        return Task.CompletedTask;
    }
}