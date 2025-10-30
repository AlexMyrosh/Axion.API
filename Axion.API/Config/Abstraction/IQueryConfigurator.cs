namespace Axion.API.Config.Abstraction;

public interface IQueryConfigurator
{
    Task InitializeAsync();
    bool TryGetQuery(string entityName, string queryName, out string queryText);
}