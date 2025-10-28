using System.Globalization;
using System.Text.Json;
using Axion.API.Models;
using Axion.API.Validation.Validators.Abstractions;

namespace Axion.API.Validation.Validators;

public class DecimalFieldValidator : BaseValidator
{
    public override ValidationError? Validate(JsonElement fieldValue, RequestField field)
    {
        decimal value;
        
        if (fieldValue.ValueKind == JsonValueKind.Number)
        {
            if (!fieldValue.TryGetDecimal(out value))
            {
                return new ValidationError
                {
                    Field = field.Name,
                    Code = "invalid_type",
                    Message = $"Field '{field.Name}' must be a valid decimal number"
                };
            }
        }
        else if (fieldValue.ValueKind == JsonValueKind.String)
        {
            var strValue = fieldValue.GetString() ?? string.Empty;
            if (!decimal.TryParse(strValue, CultureInfo.InvariantCulture, out value))
            {
                return new ValidationError
                {
                    Field = field.Name,
                    Code = "invalid_type",
                    Message = $"Field '{field.Name}' must be a valid decimal number"
                };
            }
        }
        else
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "invalid_type",
                Message = $"Field '{field.Name}' must be a decimal number"
            };
        }

        return ValidateMinMax(value, field);
    }
}

