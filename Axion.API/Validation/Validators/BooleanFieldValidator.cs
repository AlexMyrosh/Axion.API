using System.Text.Json;
using Axion.API.Models;
using Axion.API.Validation.Validators.Abstractions;

namespace Axion.API.Validation.Validators;

public class BooleanFieldValidator : IFieldValidator
{
    public ValidationError? Validate(JsonElement fieldValue, RequestField field)
    {
        bool value;
        
        if (fieldValue.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            value = fieldValue.GetBoolean();
        }
        else if (fieldValue.ValueKind == JsonValueKind.String)
        {
            var strValue = fieldValue.GetString()?.ToLowerInvariant();
            if (strValue is "true" or "false")
            {
                value = strValue == "true";
            }
            else
            {
                return new ValidationError
                {
                    Field = field.Name,
                    Code = "invalid_type",
                    Message = $"Field '{field.Name}' must be a boolean"
                };
            }
        }
        else
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "invalid_type",
                Message = $"Field '{field.Name}' must be a boolean"
            };
        }

        if (field.AllowedValues is { Count: > 0 })
        {
            var stringValue = value.ToString().ToLowerInvariant();
            if (!field.AllowedValues.Contains(stringValue))
            {
                return new ValidationError
                {
                    Field = field.Name,
                    Code = "invalid_value",
                    Message = $"Field '{field.Name}' must be one of: {string.Join(", ", field.AllowedValues)}"
                };
            }
        }

        return null;
    }
}

