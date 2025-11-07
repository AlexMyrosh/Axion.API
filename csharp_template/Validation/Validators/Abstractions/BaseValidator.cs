using System.Text.Json;
using csharp_template.Models;

namespace csharp_template.Validation.Validators.Abstractions;

public abstract class BaseValidator : IFieldValidator
{
    public abstract ValidationError? Validate(JsonElement fieldValue, RequestField field);
    
    protected static ValidationError? ValidateMinMax(decimal value, RequestField field)
    {
        if (field.Min.HasValue && value < field.Min.Value)
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "min_value",
                Message = $"Field '{field.Name}' must be at least {field.Min.Value}"
            };
        }

        if (field.Max.HasValue && value > field.Max.Value)
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
