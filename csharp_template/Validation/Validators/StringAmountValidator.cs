using System.Text.Json;
using csharp_template.Models;

namespace csharp_template.Validation.Validators;

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