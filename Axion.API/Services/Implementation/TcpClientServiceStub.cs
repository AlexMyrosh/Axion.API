using Axion.API.Services.Abstraction;

namespace Axion.API.Services.Implementation;

public class TcpClientServiceStub : ITcpClientService
{
    public Task SendAsync(string message)
    {
        Console.WriteLine($"[TcpClientStub] Sent: {message}");
        return Task.CompletedTask;
    }
}