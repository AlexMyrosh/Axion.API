using System.Globalization;
using System.Text.Json;
using csharp_template.Models;
using csharp_template.Validation.Validators.Abstractions;

namespace csharp_template.Validation.Validators;

public class NumberAmountValidator : BaseValidator
{
    public override ValidationError? Validate(JsonElement fieldValue, RequestField field)
    {
        return ValidateNumberAmount(fieldValue, field, false);
    }

    protected ValidationError? ValidateNumberAmount(JsonElement fieldValue, RequestField field, bool shouldAcceptString)
    {
        decimal value = 0;
        string rawValue;
        
        if (fieldValue.ValueKind == JsonValueKind.Number)
        {
            rawValue = fieldValue.GetRawText();
            
            if (!fieldValue.TryGetDecimal(out value))
            {
                return new ValidationError
                {
                    Field = field.Name,
                    Code = "invalid_type",
                    Message = $"Field '{field.Name}' must be a valid number"
                };
            }
        }
        else if (fieldValue.ValueKind == JsonValueKind.String && shouldAcceptString)
        {
            rawValue = fieldValue.GetString() ?? string.Empty;
            
            if (!decimal.TryParse(rawValue, CultureInfo.InvariantCulture, out value))
            {
                return new ValidationError
                {
                    Field = field.Name,
                    Code = "invalid_type",
                    Message = $"Field '{field.Name}' must be a valid number string"
                };
            }
        }
        else
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "invalid_type",
                Message = shouldAcceptString
                    ? $"Field '{field.Name}' must be a number or a string containing a number"
                    : $"Field '{field.Name}' must be a number, not a string"
            };
        }
        
        var decimalIndex = rawValue.IndexOf('.');
        if (decimalIndex >= 0 && decimalIndex < rawValue.Length - 1)
        {
            var decimalPart = rawValue.Substring(decimalIndex + 1);
            if (decimalPart.Length > 2)
            {
                return new ValidationError
                {
                    Field = field.Name,
                    Code = "too_many_decimal_places",
                    Message = $"Field '{field.Name}' must have no more than 2 decimal places"
                };
            }
        }

        return ValidateMinMax(value, field);
    }
}

