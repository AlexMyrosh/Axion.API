using System.Text.Json;
using System.Text.RegularExpressions;
using csharp_template.Models;
using csharp_template.Validation.Validators.Abstractions;

namespace csharp_template.Validation.Validators;

public class StringFieldValidator : IFieldValidator
{
    public ValidationError? Validate(JsonElement fieldValue, RequestField field)
    {
        if (fieldValue.ValueKind != JsonValueKind.String)
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "invalid_type",
                Message = $"Field '{field.Name}' must be a string"
            };
        }

        var value = fieldValue.GetString() ?? string.Empty;

        if (field.MaxLength.HasValue && value.Length > field.MaxLength.Value)
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "max_length_exceeded",
                Message = $"Field '{field.Name}' must not exceed {field.MaxLength.Value} characters"
            };
        }

        if (field.AllowedValues is { Count: > 0 } && !field.AllowedValues.Contains(value))
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "invalid_value",
                Message = $"Field '{field.Name}' must be one of: {string.Join(", ", field.AllowedValues)}"
            };
        }

        if (!string.IsNullOrEmpty(field.RegExp))
        {
            try
            {
                var regex = new Regex(field.RegExp);
                if (!regex.IsMatch(value))
                {
                    return new ValidationError
                    {
                        Field = field.Name,
                        Code = "pattern_mismatch",
                        Message = $"Field '{field.Name}' does not match required pattern"
                    };
                }
            }
            catch
            {
                return new ValidationError
                {
                    Field = field.Name,
                    Code = "invalid_regex",
                    Message = $"Invalid regex pattern for field '{field.Name}'"
                };
            }
        }

        return null;
    }
}

