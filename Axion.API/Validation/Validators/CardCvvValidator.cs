using System.Text.Json;
using System.Text.RegularExpressions;
using Axion.API.Models;
using Axion.API.Validation.Validators.Abstractions;

namespace Axion.API.Validation.Validators;

public class CardCvvValidator : IFieldValidator
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
                Code = "invalid_card_cvv",
                Message = $"Field '{field.Name}' must contain only digits"
            };
        }

        if (value.Length != 3)
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "invalid_card_cvv",
                Message = $"Field '{field.Name}' must be exactly 3 digits"
            };
        }

        return null;
    }
}

