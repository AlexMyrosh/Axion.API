using System.Text.Json;
using System.Text.RegularExpressions;
using Axion.API.Models;
using Axion.API.Validation.Validators.Abstractions;

namespace Axion.API.Validation.Validators;

public class CardExpireMonthValidator : IFieldValidator
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
                Code = "invalid_card_expire_month",
                Message = $"Field '{field.Name}' must contain only digits"
            };
        }

        if (value.Length != 2)
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "invalid_card_expire_month",
                Message = $"Field '{field.Name}' must be exactly 2 digits"
            };
        }

        if (int.TryParse(value, out var month) && (month < 1 || month > 12))
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "invalid_card_expire_month",
                Message = $"Field '{field.Name}' must be between 01 and 12"
            };
        }

        return null;
    }
}

