using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Axion.API.Models;

namespace Axion.API.Validation;

public class RequestValidator
{
    public List<ValidationError> Validate(JsonElement? body, RequestSchema? schema)
    {
        var errors = new List<ValidationError>();
        
        if (schema == null || schema.Fields.Count == 0)
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

    private ValidationError? ValidateField(JsonElement? body, RequestField field)
    {
        // if body is not provided and the filed is required - return error
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

        // Check if field exists in body
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

        // If field is null/undefined and not required, skip validation
        if ((fieldValue.ValueKind == JsonValueKind.Null || fieldValue.ValueKind == JsonValueKind.Undefined) && !field.Required)
        {
            return null;
        }

        // If field is null/undefined but required
        if ((fieldValue.ValueKind == JsonValueKind.Null || fieldValue.ValueKind == JsonValueKind.Undefined) && field.Required)
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "null_value",
                Message = $"Field '{field.Name}' cannot be null"
            };
        }
        
        // Type-specific validation
        return field.Type.ToLowerInvariant() switch
        {
            "string" => ValidateStringField(fieldValue, field),
            "decimal" or "float" or "double" => ValidateDecimalField(fieldValue, field),
            "integer" or "int" => ValidateIntegerField(fieldValue, field),
            "boolean" or "bool" => ValidateBooleanField(fieldValue, field),
            "card_number" => ValidateCardNumber(fieldValue, field),
            "card_expire_year" => ValidateCardExpireYear(fieldValue, field),
            "card_expire_month" => ValidateCardExpireMonth(fieldValue, field),
            "card_cvv" => ValidateCardCvv(fieldValue, field),
            "number_amount" => ValidateNumberAmount(fieldValue, field),
            "string_amount" => ValidateStringAmount(fieldValue, field),
            "array" => ValidateArrayField(fieldValue, field),
            _ => new ValidationError
            {
                Field = field.Name,
                Code = "unsupported_type",
                Message = $"Field type '{field.Type}' is not supported"
            }
        };
    }

    private ValidationError? ValidateStringField(JsonElement fieldValue, RequestField field)
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

        var value = fieldValue.GetString() ?? string.Empty;

        // MaxLength validation
        if (field.MaxLength.HasValue && value.Length > field.MaxLength.Value)
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "max_length_exceeded",
                Message = $"Field '{field.Name}' must not exceed {field.MaxLength.Value} characters"
            };
        }

        // AllowedValues validation
        if (field.AllowedValues is { Count: > 0 })
        {
            if (!field.AllowedValues.Contains(value))
            {
                return new ValidationError
                {
                    Field = field.Name,
                    Code = "invalid_value",
                    Message = $"Field '{field.Name}' must be one of: {string.Join(", ", field.AllowedValues)}"
                };
            }
        }

        // RegExp validation
        if (!string.IsNullOrEmpty(field.RegExp))
        {
            try
            {
                var regex = new Regex(field.RegExp);
                if (!regex.IsMatch(value))
                {
                    return new ValidationError
                    {
                        Field = field.Name,
                        Code = "pattern_mismatch",
                        Message = $"Field '{field.Name}' does not match required pattern"
                    };
                }
            }
            catch (Exception)
            {
                return new ValidationError
                {
                    Field = field.Name,
                    Code = "invalid_regex",
                    Message = $"Invalid regex pattern for field '{field.Name}'"
                };
            }
        }

        return null;
    }

    private ValidationError? ValidateDecimalField(JsonElement fieldValue, RequestField field)
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

        // Min validation
        if (field.Min.HasValue && value < field.Min.Value)
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "min_value",
                Message = $"Field '{field.Name}' must be at least {field.Min.Value}"
            };
        }

        // Max validation
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

    private ValidationError? ValidateIntegerField(JsonElement fieldValue, RequestField field)
    {
        long value;
        
        if (fieldValue.ValueKind == JsonValueKind.Number)
        {
            if (!fieldValue.TryGetInt64(out value))
            {
                return new ValidationError
                {
                    Field = field.Name,
                    Code = "invalid_type",
                    Message = $"Field '{field.Name}' must be a valid integer"
                };
            }
        }
        else if (fieldValue.ValueKind == JsonValueKind.String)
        {
            var strValue = fieldValue.GetString() ?? string.Empty;
            if (!long.TryParse(strValue, CultureInfo.InvariantCulture, out value))
            {
                return new ValidationError
                {
                    Field = field.Name,
                    Code = "invalid_type",
                    Message = $"Field '{field.Name}' must be a valid integer"
                };
            }
        }
        else
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "invalid_type",
                Message = $"Field '{field.Name}' must be an integer"
            };
        }

        // Min validation (convert to long for comparison)
        if (field.Min.HasValue && value < (long)field.Min.Value)
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "min_value",
                Message = $"Field '{field.Name}' must be at least {field.Min.Value}"
            };
        }

        // Max validation
        if (field.Max.HasValue && value > (long)field.Max.Value)
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

    private ValidationError? ValidateBooleanField(JsonElement fieldValue, RequestField field)
    {
        bool value;
        
        if (fieldValue.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            value = fieldValue.GetBoolean();
        }
        else if (fieldValue.ValueKind == JsonValueKind.String)
        {
            var strValue = fieldValue.GetString()?.ToLowerInvariant();
            if (strValue is "true" or "false")
            {
                value = strValue == "true";
            }
            else
            {
                return new ValidationError
                {
                    Field = field.Name,
                    Code = "invalid_type",
                    Message = $"Field '{field.Name}' must be a boolean"
                };
            }
        }
        else
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "invalid_type",
                Message = $"Field '{field.Name}' must be a boolean"
            };
        }

        // AllowedValues validation for boolean
        if (field.AllowedValues is { Count: > 0 })
        {
            var stringValue = value.ToString().ToLowerInvariant();
            if (!field.AllowedValues.Contains(stringValue))
            {
                return new ValidationError
                {
                    Field = field.Name,
                    Code = "invalid_value",
                    Message = $"Field '{field.Name}' must be one of: {string.Join(", ", field.AllowedValues)}"
                };
            }
        }

        return null;
    }

    private ValidationError? ValidateCardNumber(JsonElement fieldValue, RequestField field)
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

        var cardNumber = fieldValue.GetString() ?? string.Empty;
        
        // Remove spaces and dashes
        cardNumber = cardNumber.Replace(" ", "").Replace("-", "");

        // Check if it contains only digits
        if (!Regex.IsMatch(cardNumber, "^[0-9]+$"))
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "invalid_card_number",
                Message = $"Field '{field.Name}' must contain only digits"
            };
        }

        // Check length (exactly 16 digits)
        if (cardNumber.Length != 16)
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "invalid_card_number",
                Message = $"Field '{field.Name}' must be exactly 16 digits"
            };
        }

        // Luhn algorithm validation
        if (!ValidateLuhn(cardNumber))
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "invalid_card_number",
                Message = $"Field '{field.Name}' is not a valid card number"
            };
        }

        return null;
    }

    private bool ValidateLuhn(string cardNumber)
    {
        var sum = 0;
        var alternate = false;
        
        for (var i = cardNumber.Length - 1; i >= 0; i--)
        {
            var digit = cardNumber[i] - '0';
            
            if (alternate)
            {
                digit *= 2;
                if (digit > 9)
                {
                    digit -= 9;
                }
            }
            
            sum += digit;
            alternate = !alternate;
        }
        
        return sum % 10 == 0;
    }

    private ValidationError? ValidateCardExpireYear(JsonElement fieldValue, RequestField field)
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

        var value = fieldValue.GetString() ?? string.Empty;

        // Check if it contains only digits
        if (!Regex.IsMatch(value, "^[0-9]+$"))
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "invalid_card_expire_year",
                Message = $"Field '{field.Name}' must contain only digits"
            };
        }

        // Check length (exactly 2 digits)
        if (value.Length != 2)
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "invalid_card_expire_year",
                Message = $"Field '{field.Name}' must be exactly 2 digits"
            };
        }

        // Check value > 22
        if (int.TryParse(value, out var year) && year <= 22)
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "invalid_card_expire_year",
                Message = $"Field '{field.Name}' must be greater than 22"
            };
        }

        return null;
    }

    private ValidationError? ValidateCardExpireMonth(JsonElement fieldValue, RequestField field)
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

        var value = fieldValue.GetString() ?? string.Empty;

        // Check if it contains only digits
        if (!Regex.IsMatch(value, "^[0-9]+$"))
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "invalid_card_expire_month",
                Message = $"Field '{field.Name}' must contain only digits"
            };
        }

        // Check length (exactly 2 digits)
        if (value.Length != 2)
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "invalid_card_expire_month",
                Message = $"Field '{field.Name}' must be exactly 2 digits"
            };
        }

        // Check value from 01 to 12
        if (int.TryParse(value, out var month) && (month < 1 || month > 12))
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "invalid_card_expire_month",
                Message = $"Field '{field.Name}' must be between 01 and 12"
            };
        }

        return null;
    }

    private ValidationError? ValidateCardCvv(JsonElement fieldValue, RequestField field)
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

        var value = fieldValue.GetString() ?? string.Empty;

        // Check if it contains only digits
        if (!Regex.IsMatch(value, "^[0-9]+$"))
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "invalid_card_cvv",
                Message = $"Field '{field.Name}' must contain only digits"
            };
        }

        // Check length (exactly 3 digits)
        if (value.Length != 3)
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "invalid_card_cvv",
                Message = $"Field '{field.Name}' must be exactly 3 digits"
            };
        }

        return null;
    }

    private ValidationError? ValidateNumberAmount(JsonElement fieldValue, RequestField field, bool shouldAcceptString = false)
    {
        decimal value = 0;
        string rawValue;
        if (fieldValue.ValueKind == JsonValueKind.Number)
        {
            rawValue = fieldValue.GetRawText();
            
            if (!fieldValue.TryGetDecimal(out value))
            {
                return new ValidationError
                {
                    Field = field.Name,
                    Code = "invalid_type",
                    Message = $"Field '{field.Name}' must be a valid number"
                };
            }
        }
        else if (fieldValue.ValueKind == JsonValueKind.String && shouldAcceptString)
        {
            rawValue = fieldValue.GetString() ?? string.Empty;
            
            if (!decimal.TryParse(rawValue, CultureInfo.InvariantCulture, out value))
            {
                return new ValidationError
                {
                    Field = field.Name,
                    Code = "invalid_type",
                    Message = $"Field '{field.Name}' must be a valid number string"
                };
            }
        }
        else
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "invalid_type",
                Message = shouldAcceptString
                    ? $"Field '{field.Name}' must be a number or a string containing a number"
                    : $"Field '{field.Name}' must be a number, not a string"
            };
        }
        
        var decimalIndex = rawValue.IndexOf('.');
        if (decimalIndex >= 0 && decimalIndex < rawValue.Length - 1)
        {
            var decimalPart = rawValue.Substring(decimalIndex + 1);
            if (decimalPart.Length > 2)
            {
                return new ValidationError
                {
                    Field = field.Name,
                    Code = "too_many_decimal_places",
                    Message = $"Field '{field.Name}' must have no more than 2 decimal places"
                };
            }
        }

        // Min validation
        if (field.Min.HasValue && value < field.Min.Value)
        {
            return new ValidationError
            {
                Field = field.Name,
                Code = "min_value",
                Message = $"Field '{field.Name}' must be at least {field.Min.Value}"
            };
        }

        // Max validation
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

    private ValidationError? ValidateStringAmount(JsonElement fieldValue, RequestField field)
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

    private ValidationError? ValidateArrayField(JsonElement fieldValue, RequestField field)
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
                var error = ValidateField(fieldValue, nestedField);
                if (error != null)
                {
                    // Prefix the field name with the parent field name for better error context
                    error.Field = $"{field.Name}.{error.Field}";
                    return error;
                }
            }
        }

        return null;
    }
}