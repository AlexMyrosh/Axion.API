using System.Text.Json;

namespace Axion.API.Utilities;

public static class RequestDataExtractor
{
    public enum DataType
    {
        String,
        Int,
        Long,
        Decimal,
        Boolean
    }

    public static object? GetValue(string key, JsonElement? source, DataType dataType = DataType.String, object? defaultValue = null)
    {
        if (!source.HasValue || !source.Value.TryGetProperty(key, out var property))
        {
            return defaultValue;
        }

        try
        {
            return dataType switch
            {
                DataType.String => property.GetString() ?? string.Empty,
                DataType.Int => property.GetInt32(),
                DataType.Long => property.GetInt64(),
                DataType.Decimal => property.GetDecimal(),
                DataType.Boolean => property.GetBoolean(),
                _ => defaultValue
            };
        }
        catch
        {
            return defaultValue;
        }
    }
}