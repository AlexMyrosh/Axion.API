using System.Text.Json;
using csharp_template.Models;
using csharp_template.Validation.Validators.Abstractions;

namespace csharp_template.Validation.Validators;

public class ArrayFieldValidator(IRequestFieldValidator requestFieldValidator) : IFieldValidator
{
    public ValidationError? Validate(JsonElement fieldValue, RequestField field)
    {
        if (fieldValue.ValueKind != JsonValueKind.Object)
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "invalid_type",
                Message = $"Field '{field.Name}' must be an object"
            };
        }
        
        if (field.Fields is { Count: > 0 })
        {
            foreach (var nestedField in field.Fields)
            {
                var error = requestFieldValidator.ValidateField(fieldValue, nestedField);
                if (error != null)
                {
                    error.Field = $"{field.Name}.{error.Field}";
                    return error;
                }
            }
        }

        return null;
    }
}
