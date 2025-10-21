namespace Axion.API.Auth;

public interface IAuthProvider
{
    bool Validate(string token);
}