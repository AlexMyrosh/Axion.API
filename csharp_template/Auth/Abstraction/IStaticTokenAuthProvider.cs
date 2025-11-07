namespace csharp_template.Auth.Abstraction;

public interface IStaticTokenAuthProvider
{
    public bool Validate(string token);
}