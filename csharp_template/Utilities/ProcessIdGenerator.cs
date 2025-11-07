namespace csharp_template.Utilities;

public static class ProcessIdGenerator
{
    public static string Generate()
    {
        return Guid.NewGuid().ToString("N")[..8];
    }
}