using System.Collections.Concurrent;
using System.Text.Json;
using csharp_template.Config.Abstraction;
using csharp_template.Models;

namespace csharp_template.Config;

public class QueryConfigurator(ILogger<QueryConfigurator> logger) : IQueryConfigurator
{
    private readonly ConcurrentDictionary<string, string> _queries = new();
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
        var duplicates = new List<string>();
        var totalQueries = 0;
        var addedQueries = 0;

        foreach (var filePath in jsonFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);

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

                var addedFromFile = 0;
                foreach (var (queryName, queryText) in queryFile.Queries)
                {
                    totalQueries++;
                    if (_queries.TryAdd(queryName, queryText))
                    {
                        addedQueries++;
                        addedFromFile++;
                        continue;
                    }

                    duplicates.Add($"{queryName} (file {fileName}.json)");
                }

                logger.LogInformation("Processed {FileName}: added {Added} of {Total} queries", $"{fileName}.json", addedFromFile, queryFile.Queries.Count);
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

        if (duplicates.Count > 0)
        {
            logger.LogWarning("Skipped duplicate queries: {Duplicates}", string.Join(", ", duplicates));
        }

        IsInitialized = true;
        logger.LogInformation("Query registry initialized. Added {Added} of {Total} queries. Unique queries: {UniqueCount}", addedQueries, totalQueries, _queries.Count);
    }

    public bool TryGetQuery(string queryName, out string queryText)
    {
        queryText = string.Empty;
        if (string.IsNullOrWhiteSpace(queryName))
        {
            return false;
        }

        if (!_queries.TryGetValue(queryName, out var found) || string.IsNullOrWhiteSpace(found))
        {
            return false;
        }

        queryText = found;
        return true;
    }
}