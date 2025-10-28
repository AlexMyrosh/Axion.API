namespace Axion.API.Models;

public class RequestField
{
    public string Name { get; set; } = string.Empty;
    
    public string Type { get; set; } = string.Empty;
    
    public bool Required { get; set; }
    
    public decimal? Min { get; set; }
    
    public decimal? Max { get; set; }
    
    public int? MaxLength { get; set; }
    
    public string? RegExp { get; set; }
    
    public List<string>? AllowedValues { get; set; }
    
    public List<RequestField>? Fields { get; set; }
}