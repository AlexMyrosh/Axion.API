using System.Text.Json;

namespace Axion.API.Auth.Abstraction;

public interface IJwtAuthProvider
{
    bool Validate(string token, JsonElement? body);
}