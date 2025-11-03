using System.Text.Json;
using Axion.API.Models;
using Axion.API.Validation.Validators;

namespace Axion.API.Validation;

public class RequestValidator : IRequestFieldValidator
{
    private readonly ValidatorFactory _validatorFactory;

    public RequestValidator()
    {
        _validatorFactory = new ValidatorFactory(this);
    }

    public List<ValidationError> Validate(JsonElement? body, RequestSchema? schema)
    {
        var errors = new List<ValidationError>();
        if (schema == null || schema.Fields == null || schema.Fields.Count == 0)
        {
            return errors;
        }

        foreach (var field in schema.Fields)
        {
            var error = ValidateField(body, field);
            if (error != null)
            {
                errors.Add(error);
            }
        }

        return errors;
    }

    public ValidationError? ValidateField(JsonElement? body, RequestField field)
    {
        if (body == null || body.Value.ValueKind == JsonValueKind.Null || body.Value.ValueKind == JsonValueKind.Undefined)
        {
            if (field.Required)
            {
                return new ValidationError
                {
                    Field = field.Name,
                    Code = "missing_field",
                    Message = $"Field '{field.Name}' is required"
                };
            }
            
            return null;
        }

        if (!body.Value.TryGetProperty(field.Name, out var fieldValue))
        {
            if (field.Required)
            {
                return new ValidationError
                {
                    Field = field.Name,
                    Code = "missing_field",
                    Message = $"Field '{field.Name}' is required"
                };
            }
            
            return null;
        }

        if ((fieldValue.ValueKind == JsonValueKind.Null || fieldValue.ValueKind == JsonValueKind.Undefined) && !field.Required)
        {
            return null;
        }

        if ((fieldValue.ValueKind == JsonValueKind.Null || fieldValue.ValueKind == JsonValueKind.Undefined) && field.Required)
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "null_value",
                Message = $"Field '{field.Name}' cannot be null"
            };
        }
        
        // Get appropriate validator for the field type
        var validator = _validatorFactory.GetValidator(field.Type);
        if (validator != null)
        {
            return validator.Validate(fieldValue, field);
        }

        return new ValidationError
        {
            Field = field.Name,
            Code = "unsupported_type",
            Message = $"Field type '{field.Type}' is not supported"
        };
    }
}
