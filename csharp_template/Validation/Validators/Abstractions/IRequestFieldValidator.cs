using System.Text.Json;
using csharp_template.Models;

namespace csharp_template.Validation;

public interface IRequestFieldValidator
{
    ValidationError? ValidateField(JsonElement? body, RequestField field);
}