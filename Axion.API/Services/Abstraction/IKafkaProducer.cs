namespace Axion.API.Services.Abstraction;

public interface IKafkaProducer
{
    Task ProduceAsync(string topic, string message);
}