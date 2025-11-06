using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Axion.API.HttpClient.Abstraction;
using Axion.API.Models;

namespace Axion.API.HttpClient.Implementation;

public class HttpClientWrapper(IHttpClientFactory httpClientFactory, ILogger<HttpClientWrapper> logger) : IHttpClientWrapper
{
    private const string ContentTypeHeader = "content-type";
    private const string DefaultContentType = "application/json";
    private const int DefaultTimeout = 40000;
    private const int TimeoutStatusCode = 408;
    private const int ErrorStatusCode = 500;
    private const string TimeoutMessage = "Request timeout";
    
    private readonly Dictionary<string, string> _defaultHeaders = new() { [ContentTypeHeader] = DefaultContentType };

    public async Task<HttpClientResponse> SendRequestAsync(HttpMethod method, string url, object? body = null, IDictionary<string, string>? headers = null, IDictionary<string, int>? options = null)
    {
        logger.LogInformation("Sending {Method} request to {Url}", method, url);
        var response = new HttpClientResponse
        {
            IsSuccess = false,
            ResponseCode = 0,
            ResponseHeaders = new Dictionary<string, string>(),
            ResponseRaw = string.Empty,
            ResponseParsedJson = null
        };

        try
        {
            var finalHeaders = MergeHeaders(headers);
            var request = CreateHttpRequest(method, url, body, finalHeaders);
            var timeout = options?.TryGetValue("timeout", out var timeoutValue) == true ? timeoutValue : DefaultTimeout;
            
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMilliseconds(timeout);
            var httpResponse = await client.SendAsync(request);
            
            await PopulateResponse(response, httpResponse);

            logger.LogInformation("Request completed with status code {StatusCode}", response.ResponseCode);
            return response;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Request timeout for {Url}", url);
            return CreateErrorResponse(TimeoutStatusCode, TimeoutMessage);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request error for {Url}", url);
            return CreateErrorResponse(ErrorStatusCode, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during HTTP request to {Url}", url);
            return CreateErrorResponse(ErrorStatusCode, ex.Message);
        }
    }

    private static HttpClientResponse CreateErrorResponse(int statusCode, string message) => new()
    {
        IsSuccess = false,
        ResponseCode = statusCode,
        ResponseHeaders = new Dictionary<string, string>(),
        ResponseRaw = message,
        ResponseParsedJson = null
    };

    private Dictionary<string, string> MergeHeaders(IDictionary<string, string>? headers)
    {
        if (headers == null) return _defaultHeaders;
        
        var finalHeaders = new Dictionary<string, string>(headers);
        foreach (var header in _defaultHeaders.Where(h => !headers.ContainsKey(h.Key)))
        {
            finalHeaders[header.Key] = header.Value;
        }
        
        return finalHeaders;
    }

    private static HttpRequestMessage CreateHttpRequest(HttpMethod method, string url, object? body, IDictionary<string, string> headers)
    {
        var request = new HttpRequestMessage(method, url);

        foreach (var header in headers.Where(h => !h.Key.Equals(ContentTypeHeader, StringComparison.OrdinalIgnoreCase)))
        {
            request.Headers.Add(header.Key, header.Value);
        }

        if (body != null && (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch))
        {
            var bodyJson = body as string ?? JsonSerializer.Serialize(body);
            request.Content = new StringContent(bodyJson, Encoding.UTF8);
            if (headers.TryGetValue(ContentTypeHeader, out var contentType))
            {
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            }
        }

        return request;
    }

    private async Task PopulateResponse(HttpClientResponse response, HttpResponseMessage httpResponse)
    {
        var responseContent = await httpResponse.Content.ReadAsStringAsync();
        response.ResponseCode = (int)httpResponse.StatusCode;
        response.IsSuccess = httpResponse.IsSuccessStatusCode;
        response.ResponseRaw = responseContent;

        foreach (var header in httpResponse.Content.Headers)
        {
            response.ResponseHeaders[header.Key] = string.Join(", ", header.Value);
        }

        if (!string.IsNullOrWhiteSpace(responseContent))
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(responseContent);
                response.ResponseParsedJson = jsonDoc.RootElement.Clone();
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Failed to parse response as JSON");
            }
        }
    }
}