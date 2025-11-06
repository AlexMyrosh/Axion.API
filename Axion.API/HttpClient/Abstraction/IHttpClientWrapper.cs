using Axion.API.Models;

namespace Axion.API.HttpClient.Abstraction;

public interface IHttpClientWrapper
{
    Task<HttpClientResponse> SendRequestAsync(
        HttpMethod method,
        string url,
        object? body = null,
        IDictionary<string, string>? headers = null,
        IDictionary<string, int>? options = null);
}