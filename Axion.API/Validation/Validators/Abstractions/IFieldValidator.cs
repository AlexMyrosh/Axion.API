using System.Text.Json;
using Axion.API.Models;

namespace Axion.API.Validation.Validators.Abstractions;

public interface IFieldValidator
{
    ValidationError? Validate(JsonElement fieldValue, RequestField field);
}
