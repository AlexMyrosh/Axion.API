namespace Axion.API.Auth;

public class StaticTokenAuthProvider
{
    private readonly Dictionary<string, string> _tokens = new();

    public StaticTokenAuthProvider(IConfiguration config)
    {
        var section = config.GetSection("StaticTokens");
        foreach (var kvp in section.GetChildren())
        {
            _tokens[kvp.Key] = kvp.Value ?? string.Empty;
        }
    }

    public bool Validate(string token)
    {
        return _tokens.Values.Any(v => v == token);
    }
}