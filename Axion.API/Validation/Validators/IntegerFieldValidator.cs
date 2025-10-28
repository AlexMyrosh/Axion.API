using System.Globalization;
using System.Text.Json;
using Axion.API.Models;
using Axion.API.Validation.Validators.Abstractions;

namespace Axion.API.Validation.Validators;

public class IntegerFieldValidator : BaseValidator
{
    public override ValidationError? Validate(JsonElement fieldValue, RequestField field)
    {
        long value;
        
        if (fieldValue.ValueKind == JsonValueKind.Number)
        {
            if (!fieldValue.TryGetInt64(out value))
            {
                return new ValidationError
                {
                    Field = field.Name,
                    Code = "invalid_type",
                    Message = $"Field '{field.Name}' must be a valid integer"
                };
            }
        }
        else if (fieldValue.ValueKind == JsonValueKind.String)
        {
            var strValue = fieldValue.GetString() ?? string.Empty;
            if (!long.TryParse(strValue, CultureInfo.InvariantCulture, out value))
            {
                return new ValidationError
                {
                    Field = field.Name,
                    Code = "invalid_type",
                    Message = $"Field '{field.Name}' must be a valid integer"
                };
            }
        }
        else
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "invalid_type",
                Message = $"Field '{field.Name}' must be an integer"
            };
        }

        if (field.Min.HasValue && value < (long)field.Min.Value)
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "min_value",
                Message = $"Field '{field.Name}' must be at least {field.Min.Value}"
            };
        }

        if (field.Max.HasValue && value > (long)field.Max.Value)
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "max_value",
                Message = $"Field '{field.Name}' must not exceed {field.Max.Value}"
            };
        }

        return null;
    }
}

