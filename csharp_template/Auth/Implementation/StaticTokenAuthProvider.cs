using csharp_template.Auth.Abstraction;

namespace csharp_template.Auth.Implementation;

public class StaticTokenAuthProvider : IStaticTokenAuthProvider
{
    private readonly HashSet<string> _tokens = new();

    public StaticTokenAuthProvider(IConfiguration config)
    {
        var section = config.GetSection("StaticTokens");
        foreach (var kvp in section.GetChildren())
        {
            if (!string.IsNullOrEmpty(kvp.Value))
            {
                _tokens.Add(kvp.Value);
            }
        }
    }

    public bool Validate(string token)
    {
        return !string.IsNullOrEmpty(token) && _tokens.Contains(token);
    }
}