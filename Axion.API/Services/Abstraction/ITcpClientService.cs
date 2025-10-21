namespace Axion.API.Services.Abstraction;

public interface ITcpClientService
{
    Task SendAsync(string message);
}