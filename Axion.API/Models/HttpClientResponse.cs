using System.Text.Json;

namespace Axion.API.Models;

public class HttpClientResponse
{
    public bool IsSuccess { get; set; }
    
    public int ResponseCode { get; set; }
    
    public IDictionary<string, string> ResponseHeaders { get; set; } = new Dictionary<string, string>();
    
    public string ResponseRaw { get; set; } = string.Empty;
    
    public JsonElement? ResponseParsedJson { get; set; }
}