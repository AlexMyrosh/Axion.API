namespace Axion.API.Config.Abstraction;

public interface IQueryConfigurator
{
    bool IsInitialized { get; }
    
    Task InitializeAsync();
    bool TryGetQuery(string queryName, out string queryText);
}