namespace Axion.API.Utilities;

public static class TimestampGenerator
{
    public static string GenerateString()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
    }

    public static int GenerateInteger()
    {
        return (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}