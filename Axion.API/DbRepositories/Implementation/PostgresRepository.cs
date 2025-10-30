using System.Collections.Concurrent;
using Axion.API.Config.Abstraction;
using Axion.API.DbRepositories.Abstraction;
using Npgsql;

namespace Axion.API.DbRepositories.Implementation;

public class PostgresRepository(IConfiguration configuration, ILogger<PostgresRepository> logger, IQueryConfigurator queryConfigurator) : IPostgresRepository, IAsyncDisposable
{
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly ConcurrentDictionary<string, NpgsqlDataSource> _poolNameToDataSource = new();
    private string? _defaultPoolName;
    
    public string? ActivePoolName { get; private set; }
    public bool IsInit { get; private set; }
    public bool IsProgress { get; private set; }

    public async Task<bool> InitializeAsync()
    {
        await _initLock.WaitAsync();
        try
        {
            if (IsInit)
            {
                logger.LogWarning("Initialization already completed; skipping new attempt");
                return false;
            }

            if (IsProgress)
            {
                logger.LogWarning("Initialization already in progress; skipping new attempt");
                return false;
            }

            IsProgress = true;

            try
            {
                _defaultPoolName = configuration["ConnectionStrings:Postgres:defaultPool"]?.ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(_defaultPoolName))
                {
                    logger.LogWarning("Default pool name is missing");
                    IsInit = false;
                    return false;
                }

                var poolsSection = configuration.GetSection("ConnectionStrings:Postgres:pools");
                if (!poolsSection.Exists())
                {
                    logger.LogWarning("No pools configured under ConnectionStrings:Postgres:pools");
                    IsInit = false;
                    return false;
                }

                var expectedPools = new List<string>();
                foreach (var pool in poolsSection.GetChildren())
                {
                    var poolKey = pool.Key.ToLowerInvariant();
                    var connectionString = pool.Value;
                    if (string.IsNullOrWhiteSpace(connectionString))
                    {
                        logger.LogWarning("Empty connection string for pool '{Pool}'", poolKey);
                        continue;
                    }

                    expectedPools.Add(poolKey);
                    var dataSource = await CreateDataSourceWithRetryAsync(connectionString, poolKey);
                    if (dataSource != null)
                    {
                        _poolNameToDataSource[poolKey] = dataSource;
                    }
                }

                if (expectedPools.Count == 0)
                {
                    logger.LogWarning("PostgreSQL initialization incomplete: no pools configured with valid connection strings");
                    IsInit = false;
                    return false;
                }

                if (_poolNameToDataSource.Count != expectedPools.Count)
                {
                    var missing = expectedPools.Where(p => !_poolNameToDataSource.ContainsKey(p)).ToArray();
                    logger.LogWarning("PostgreSQL initialization partial: missing pools: {MissingPools}. Initialized: {InitializedPools}", string.Join(", ", missing), string.Join(", ", _poolNameToDataSource.Keys));
                    IsInit = false;
                    return false;
                }

                IsInit = true;
                ActivePoolName = _defaultPoolName;
                logger.LogInformation("PostgreSQL initialized. All pools ready: {Pools}. Active: {Active}", string.Join(", ", _poolNameToDataSource.Keys), ActivePoolName);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during PostgreSQL initialization");
                IsInit = false;
                return false;
            }
        }
        finally
        {
            IsProgress = false;
            _initLock.Release();
        }
    }

    public async Task<List<Dictionary<string, object?>>?> DbExecuteAsync(string entityName, string queryName, Dictionary<string, object>? parameters = null)
    {
        if (string.IsNullOrWhiteSpace(entityName) || string.IsNullOrWhiteSpace(queryName))
        {
            logger.LogWarning("Entity or query name is empty");
            return null;
        }
        
        if (!IsInit)
        {
            logger.LogWarning("PostgresRepository is not initialized");
            return null;
        }

        if (!queryConfigurator.TryGetQuery(entityName.ToLowerInvariant(), queryName, out var sql) || string.IsNullOrWhiteSpace(sql))
        {
            logger.LogWarning("Query '{Query}' not found for entity '{Entity}'", queryName, entityName);
            return null;
        }

        var dataSource = GetActiveConnection();
        if (dataSource == null)
        {
            return null;
        }

        try
        {
            await using var connection = await dataSource.OpenConnectionAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            if (parameters != null)
            {
                foreach (var (parameterName, parameterValue) in parameters)
                {
                    command.Parameters.AddWithValue($"@{parameterName}", parameterValue);
                }
            }

            await using var reader = await command.ExecuteReaderAsync();
            var rows = new List<Dictionary<string, object?>>();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    row[columnName] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                
                rows.Add(row);
            }
            
            return rows;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing query {Entity}.{Query} on pool {Pool}", entityName, queryName, ActivePoolName);
            return null;
        }
    }

    public NpgsqlDataSource? GetConnection(string poolName)
    {
        if (!IsInit)
        {
            logger.LogWarning("Attempt to get connection before initialization");
            return null;
        }

        var normalizedPoolName = poolName.ToLowerInvariant();
        if (!_poolNameToDataSource.TryGetValue(normalizedPoolName, out var dataSource))
        {
            logger.LogWarning("Pool '{Pool}' not found", normalizedPoolName);
            return null;
        }
        
        return dataSource;
    }

    public NpgsqlDataSource? GetActiveConnection()
    {
        if (!IsInit)
        {
            return null;
        }
        
        var poolName = ActivePoolName ?? _defaultPoolName;
        return string.IsNullOrWhiteSpace(poolName) ? null : GetConnection(poolName);
    }

    public bool SetActivePool(string poolName)
    {
        if (!IsInit)
        {
            logger.LogWarning("Attempt to set active pool before initialization");
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(poolName))
        {
            logger.LogWarning("Active pool name is empty");
            return false;
        }
        
        var normalizedPoolName = poolName.ToLowerInvariant();
        if (!_poolNameToDataSource.ContainsKey(normalizedPoolName))
        {
            logger.LogWarning("Cannot set active pool. Pool '{Pool}' is not configured", normalizedPoolName);
            return false;
        }
        
        ActivePoolName = normalizedPoolName;
        return true;
    }

    private async Task<NpgsqlDataSource?> CreateDataSourceWithRetryAsync(string connectionString, string poolName)
    {
        var delays = new[] { 2000, 5000, 10000 };
        Exception? last = null;
        foreach (var delayMs in delays)
        {
            try
            {
                var dataSource = new NpgsqlDataSourceBuilder(connectionString).Build();
                await using var connection = await dataSource.OpenConnectionAsync();
                return dataSource;
            }
            catch (Exception ex)
            {
                last = ex;
                logger.LogWarning(ex, "Connection attempt failed for pool '{Pool}'; retrying in {Delay}ms", poolName, delayMs);
                await Task.Delay(delayMs);
            }
        }
        
        logger.LogError(last, "Failed to create/test data source for pool '{Pool}'", poolName);
        return null;
    }
    
    public async ValueTask DisposeAsync()
    {
        IsInit = false;
        IsProgress = false;
        _initLock.Dispose();
        _poolNameToDataSource.Clear();
        foreach (var dataSource in _poolNameToDataSource.Values)
        {
            try
            {
                await dataSource.DisposeAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error disposing data source");
            }
        }
    }
}