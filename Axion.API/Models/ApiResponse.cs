namespace Axion.API.Models;

public class ApiResponse
{
    public int StatusCode { get; set; }
    
    public object? Data { get; set; }
    
    public static ApiResponse Success(object? data = null, string? message = null, int statusCode = 200)
    {
        return new ApiResponse
        {
            StatusCode = statusCode,
            Data = new SuccessApiResponse(data, message)
        };
    }
    
    public static ApiResponse Error(string errorCode, string message, object? data = null, string? type = "processing", int statusCode = 400)
    {
        return new ApiResponse
        {
            StatusCode = statusCode,
            Data = new ErrorApiResponse(errorCode, message, data, type)
        };
    }
}