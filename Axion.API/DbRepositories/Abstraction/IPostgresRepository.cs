using Npgsql;

namespace Axion.API.DbRepositories.Abstraction;

public interface IPostgresRepository
{
    bool IsInit { get; }
    bool IsProgress { get; }
    string? ActivePoolName { get; }
    
    Task<bool> InitializeAsync();
    public bool SetActivePool(string poolName);
    Task<List<Dictionary<string, object?>>?> DbExecuteAsync(string entityName, string queryName, Dictionary<string, object>? parameters = null);
    NpgsqlDataSource? GetConnection(string poolName);
    NpgsqlDataSource? GetActiveConnection();
}