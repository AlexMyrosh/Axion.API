namespace csharp_template.DbRepositories.Abstraction;

public interface IPostgresRepository
{
    bool IsInit { get; }
    bool IsProgress { get; }
    
    Task<bool> InitializeAsync();
    Task<List<Dictionary<string, object?>>?> DbExecuteAsync(string? poolName, string queryName, Dictionary<string, object>? parameters = null);
}