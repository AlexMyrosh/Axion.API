using Axion.API.Services.Abstraction;

namespace Axion.API.Services.Implementation;

public class TcpClientServiceStub(ILogger<TcpClientServiceStub> logger) : ITcpClientService
{
    public Task SendAsync(string message)
    {
        logger.LogInformation("TCP message sent: {Message}", message);
        return Task.CompletedTask;
    }
}