namespace Axion.API.Models;

public class ApiRoute
{
    public required string Path { get; set; } = string.Empty;
    
    public required string Method { get; set; } = string.Empty;
    
    public required string Auth { get; set; } = string.Empty;
    
    public required string Handler { get; set; } = string.Empty;
    
    public RequestSchema? RequestSchema { get; set; }
}