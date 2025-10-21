using System.Text.Json;

namespace Axion.API.Models;

public class ApiRequest
{
    public string Path { get; set; } = string.Empty;
    
    public string Method { get; set; } = string.Empty;

    public IDictionary<string, string?> Headers { get; set; } = new Dictionary<string, string?>();

    public JsonElement? Body { get; set; }

    public IDictionary<string, string?> Query { get; set; } = new Dictionary<string, string?>();
}