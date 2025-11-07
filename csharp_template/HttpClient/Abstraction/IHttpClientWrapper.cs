using csharp_template.Models;

namespace csharp_template.HttpClient.Abstraction;

public interface IHttpClientWrapper
{
    Task<HttpClientResponse> SendRequestAsync(
        HttpMethod method,
        string url,
        object? body = null,
        IDictionary<string, string>? headers = null,
        IDictionary<string, int>? options = null);
}