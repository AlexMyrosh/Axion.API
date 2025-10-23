using System.Text.Json.Serialization;

namespace Axion.API.Models;

public class StandardApiResponse
{
    public object? Data { get; set; }
    
    public string Result { get; set; } = string.Empty;
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ErrorCode { get; set; }
}

public class SuccessApiResponse : StandardApiResponse
{
    public SuccessApiResponse(object? data = null, string? message = null)
    {
        Data = data;
        Result = "ok";
        if (!string.IsNullOrEmpty(message))
        {
            Message = message;
        }
    }
}

public class ErrorApiResponse : StandardApiResponse
{
    public ErrorApiResponse(string errorCode, string message, object? data = null, string? type = "processing")
    {
        Data = data;
        Result = "error";
        Type = type;
        Message = message;
        ErrorCode = errorCode;
    }
}
