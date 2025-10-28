namespace Axion.API.Utilities;

public static class RouteKeyUtility
{
    public static string BuildRouteKey(string? path, string? method)
    {
        var normalizedPath = path?.ToLowerInvariant() ?? string.Empty;
        var normalizedMethod = method?.ToUpperInvariant() ?? string.Empty;
        return $"{normalizedPath}:{normalizedMethod}";
    }
}