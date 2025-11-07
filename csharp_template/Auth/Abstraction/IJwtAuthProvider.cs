using System.Text.Json;

namespace csharp_template.Auth.Abstraction;

public interface IJwtAuthProvider
{
    bool Validate(string token, JsonElement? body);
}