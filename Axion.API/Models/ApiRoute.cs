namespace Axion.API.Models;

public class ApiRoute
{
    public string Path { get; set; } = string.Empty;
    
    public string Method { get; set; } = string.Empty;
    
    public string Auth { get; set; } = string.Empty;
    
    public string Handler { get; set; } = string.Empty;
    
    public RequestSchema? RequestSchema { get; set; }
}