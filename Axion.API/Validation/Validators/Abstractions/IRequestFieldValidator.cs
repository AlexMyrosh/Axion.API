using Axion.API.Models;
using System.Text.Json;

namespace Axion.API.Validation;

public interface IRequestFieldValidator
{
    ValidationError? ValidateField(JsonElement? body, RequestField field);
}