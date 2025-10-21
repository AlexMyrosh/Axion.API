namespace Axion.API.Models;

public class ApiResponse
{
    public int StatusCode { get; set; }
    
    public object? Data { get; set; }
}