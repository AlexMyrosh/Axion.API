using System.Text.Json;
using System.Text.RegularExpressions;
using Axion.API.Models;
using Axion.API.Utilities;
using Axion.API.Validation.Validators.Abstractions;

namespace Axion.API.Validation.Validators;

public class CardNumberValidator : IFieldValidator
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

        var cardNumber = fieldValue.GetString() ?? string.Empty;
        cardNumber = cardNumber.Replace(" ", "").Replace("-", "");

        if (!Regex.IsMatch(cardNumber, "^[0-9]+$"))
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "invalid_card_number",
                Message = $"Field '{field.Name}' must contain only digits"
            };
        }

        if (cardNumber.Length != 16)
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "invalid_card_number",
                Message = $"Field '{field.Name}' must be exactly 16 digits"
            };
        }

        if (!LuhnValidationUtility.Validate(cardNumber))
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "invalid_card_number",
                Message = $"Field '{field.Name}' is not a valid card number"
            };
        }

        return null;
    }
}

