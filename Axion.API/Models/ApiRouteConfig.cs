namespace Axion.API.Models;

public record ApiRouteConfig(string Path, string Method, string Auth, string Handler);