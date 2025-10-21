namespace Axion.API.Services.Abstraction;

public interface IKafkaConsumer
{
    Task StartAsync();
}