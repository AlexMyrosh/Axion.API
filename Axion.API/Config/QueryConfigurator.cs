using System.Collections.Concurrent;
using System.Text.Json;
using Axion.API.Config.Abstraction;
using Axion.API.Models;

namespace Axion.API.Config;

public class QueryConfigurator(ILogger<QueryConfigurator> logger) : IQueryConfigurator
{
    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _queriesByEntity = new();
    public bool IsInitialized { get; private set; }

    public async Task InitializeAsync()
    {
        if (IsInitialized)
        {
            return;
        }

        var rootQueriesPath = Path.Combine(AppContext.BaseDirectory, "Queries");
        if (!Directory.Exists(rootQueriesPath))
        {
            logger.LogWarning("Queries root directory does not exist: {Path}", rootQueriesPath);
            IsInitialized = true;
            return;
        }

        var jsonFiles = Directory.EnumerateFiles(rootQueriesPath, "*.json", SearchOption.AllDirectories).ToArray();
        if (jsonFiles.Length == 0)
        {
            logger.LogWarning("No query files found under {Path}", rootQueriesPath);
            IsInitialized = true;
            return;
        }

        var errors = new List<string>();
        foreach (var filePath in jsonFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var entityKey = fileName.ToLowerInvariant();

            try
            {
                var jsonContent = await File.ReadAllTextAsync(filePath);
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    errors.Add($"File {fileName}.json is empty");
                    continue;
                }

                using var doc = JsonDocument.Parse(jsonContent);
                if (!doc.RootElement.TryGetProperty("Queries", out _))
                {
                    errors.Add($"File {fileName}.json has no required root 'Queries' element");
                    continue;
                }

                var queryFile = JsonSerializer.Deserialize<QueryFile>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (queryFile == null)
                {
                    errors.Add($"Failed to deserialize {fileName}.json");
                    continue;
                }

                if (queryFile.Queries.Count == 0)
                {
                    errors.Add($"File {fileName}.json has no queries in 'Queries' section");
                    continue;
                }

                var emptyQueryNames = queryFile.Queries
                    .Where(kvp => string.IsNullOrWhiteSpace(kvp.Value))
                    .Select(kvp => kvp.Key)
                    .ToList();
                if (emptyQueryNames.Count != 0)
                {
                    errors.Add($"File {fileName}.json has empty queries: {string.Join(", ", emptyQueryNames)}");
                    continue;
                }

                // Merge strategy: if entity already exists, later files override same query names
                _queriesByEntity.AddOrUpdate(entityKey,
                    _ => new Dictionary<string, string>(queryFile.Queries),
                    (_, existing) =>
                    {
                        foreach (var (key, value) in queryFile.Queries)
                        {
                            existing[key] = value;
                        }
                        
                        return existing;
                    });

                logger.LogInformation("Loaded {Count} queries from {FileName}.json", queryFile.Queries.Count, fileName);
            }
            catch (JsonException ex)
            {
                errors.Add($"Invalid JSON in {fileName}.json: {ex.Message}");
            }
            catch (Exception ex)
            {
                errors.Add($"Error loading {fileName}.json: {ex.Message}");
            }
        }

        if (errors.Any())
        {
            logger.LogError("Errors loading query files:\n{Errors}", string.Join("\n", errors));
        }

        IsInitialized = true;
        logger.LogInformation("Query registry initialized. Entities: {Count}", _queriesByEntity.Count);
    }

    public bool TryGetQuery(string entityName, string queryName, out string queryText)
    {
        queryText = string.Empty;
        var normalizedEntity = entityName.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedEntity) || string.IsNullOrWhiteSpace(queryName))
        {
            return false;
        }

        if (!_queriesByEntity.TryGetValue(normalizedEntity, out var queries))
        {
            return false;
        }
        
        if (!queries.TryGetValue(queryName, out var found) || string.IsNullOrWhiteSpace(found))
        {
            return false;
        }
        
        queryText = found;
        return true;
    }
}