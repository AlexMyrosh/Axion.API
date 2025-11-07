using System.Text.Json;
using System.Text.RegularExpressions;
using csharp_template.Models;
using csharp_template.Validation.Validators.Abstractions;

namespace csharp_template.Validation.Validators;

public class CardExpireYearValidator : IFieldValidator
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

        if (!Regex.IsMatch(value, "^[0-9]+$"))
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "invalid_card_expire_year",
                Message = $"Field '{field.Name}' must contain only digits"
            };
        }

        if (value.Length != 2)
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "invalid_card_expire_year",
                Message = $"Field '{field.Name}' must be exactly 2 digits"
            };
        }

        if (int.TryParse(value, out var year) && year <= 22)
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "invalid_card_expire_year",
                Message = $"Field '{field.Name}' must be greater than 22"
            };
        }

        return null;
    }
}

