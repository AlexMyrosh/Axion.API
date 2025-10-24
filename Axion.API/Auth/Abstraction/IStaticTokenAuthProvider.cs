namespace Axion.API.Auth.Abstraction;

public interface IStaticTokenAuthProvider
{
    public bool Validate(string token);
}