using System.Text.Json;
using Axion.API.Models;

namespace Axion.API.Validation.Validators;

public class StringAmountValidator : NumberAmountValidator
{
    public new ValidationError? Validate(JsonElement fieldValue, RequestField field)
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
        
        return ValidateNumberAmount(fieldValue, field, true);
    }
}