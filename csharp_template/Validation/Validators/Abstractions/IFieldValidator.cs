using System.Text.Json;
using csharp_template.Models;

namespace csharp_template.Validation.Validators.Abstractions;

public interface IFieldValidator
{
    ValidationError? Validate(JsonElement fieldValue, RequestField field);
}
