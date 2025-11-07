using System.Text.RegularExpressions;
using csharp_template.Models;

namespace csharp_template.Validation;

public class ApiRouteValidator(ILogger<ApiRouteValidator> logger)
{
    public bool ValidateRoute(ApiRoute route, int routeIndex, out List<string> errors)
    {
        errors = [];
        var routeIdentifier = $"Route #{routeIndex}";

        // Validate Path
        if (string.IsNullOrWhiteSpace(route.Path))
        {
            errors.Add($"{routeIdentifier}: 'Path' is required and cannot be empty");
        }
        else
        {
            routeIdentifier = $"Route '{route.Path}'";
        }

        // Validate Method
        if (string.IsNullOrWhiteSpace(route.Method))
        {
            errors.Add($"{routeIdentifier}: 'Method' is required and cannot be empty");
        }
        else
        {
            var validMethods = new[] { "GET", "POST", "PUT", "PATCH", "DELETE"};
            var normalizedMethod = route.Method.ToUpperInvariant();
            if (!validMethods.Contains(normalizedMethod))
            {
                errors.Add($"{routeIdentifier}: 'Method' must be one of: {string.Join(", ", validMethods)}. Got: '{route.Method}'");
            }
            else
            {
                routeIdentifier = $"Route '{route.Method} {route.Path}'";
            }
        }

        // Validate Auth
        if (string.IsNullOrWhiteSpace(route.Auth))
        {
            errors.Add($"{routeIdentifier}: 'Auth' is required and cannot be empty");
        }
        else
        {
            var validAuthTypes = new[] { "none", "jwt", "static_tokens" };
            var normalizedAuth = route.Auth.ToLowerInvariant();
            if (!validAuthTypes.Contains(normalizedAuth))
            {
                errors.Add($"{routeIdentifier}: 'Auth' must be one of: {string.Join(", ", validAuthTypes)}. Got: '{route.Auth}'");
            }
        }

        // Validate Handler
        if (string.IsNullOrWhiteSpace(route.Handler))
        {
            errors.Add($"{routeIdentifier}: 'Handler' is required and cannot be empty");
        }
        else
        {
            // Try to resolve handler type
            var handlerType = Type.GetType(route.Handler);
            if (handlerType == null)
            {
                errors.Add($"{routeIdentifier}: Handler type not found: '{route.Handler}'. Make sure the type exists and the assembly is referenced.");
            }
        }

        // Validate RequestSchema if present
        if (route.RequestSchema is { Fields: not null })
        {
            ValidateRequestSchema(route.RequestSchema, routeIdentifier, errors);
        }

        if (errors.Count <= 0)
        {
            return true;
        }
        
        foreach (var error in errors)
        {
            logger.LogError("API Route validation error: {Error}", error);
        }
        
        return false;
    }

    private void ValidateRequestSchema(RequestSchema schema, string routeIdentifier, List<string> errors)
    {
        if (schema.Fields == null || schema.Fields.Count == 0)
        {
            return; // Empty schema is valid
        }

        for (var i = 0; i < schema.Fields.Count; i++)
        {
            var fieldIdentifier = $"Field #{i}";
            ValidateRequestField(schema.Fields[i], routeIdentifier, fieldIdentifier, errors);
        }
    }

    private void ValidateRequestField(RequestField field, string routeIdentifier, string fieldIdentifier, List<string> errors)
    {
        // Validate Name
        if (string.IsNullOrWhiteSpace(field.Name))
        {
            errors.Add($"{routeIdentifier} -> {fieldIdentifier}: 'Name' is required and cannot be empty");
        }
        else
        {
            fieldIdentifier = $"Field '{field.Name}'";
        }

        // Validate Type
        if (string.IsNullOrWhiteSpace(field.Type))
        {
            errors.Add($"{routeIdentifier} -> {fieldIdentifier}: 'Type' is required and cannot be empty");
        }
        else
        {
            var validTypes = new[]
            {
                "string", "integer", "int", "decimal", "float", "double", 
                "boolean", "bool", "array",
                "card_number", "card_expire_year", "card_expire_month", "card_cvv",
                "number_amount", "string_amount"
            };
            
            var normalizedType = field.Type.ToLowerInvariant();
            if (!validTypes.Contains(normalizedType))
            {
                errors.Add($"{routeIdentifier} -> {fieldIdentifier}: 'Type' must be one of supported types. Got: '{field.Type}'. Supported: {string.Join(", ", validTypes)}");
            }
        }
        
        // Validate nested fields for array type
        if (field.Type.Equals("array", StringComparison.InvariantCultureIgnoreCase) && field.Fields is { Count: > 0 })
        {
            for (var i = 0; i < field.Fields.Count; i++)
            {
                var nestedFieldIdentifier = $"{fieldIdentifier} -> Nested Field #{i}";
                ValidateRequestField(field.Fields[i], routeIdentifier, nestedFieldIdentifier, errors);
            }
        }

        // Validate Min/Max consistency
        if (field is { Min: not null, Max: not null } && field.Min.Value > field.Max.Value)
        {
            errors.Add($"{routeIdentifier} -> {fieldIdentifier}: 'Min' ({field.Min}) cannot be greater than 'Max' ({field.Max})");
        }

        // Validate MaxLength is positive
        if (field.MaxLength is < 0)
        {
            errors.Add($"{routeIdentifier} -> {fieldIdentifier}: 'MaxLength' must be a positive number. Got: {field.MaxLength}");
        }

        // Validate RegExp if present
        if (!string.IsNullOrWhiteSpace(field.RegExp))
        {
            try
            {
                _ = new Regex(field.RegExp);
            }
            catch (Exception ex)
            {
                errors.Add($"{routeIdentifier} -> {fieldIdentifier}: 'RegExp' is not a valid regular expression. Error: {ex.Message}");
            }
        }

        // Validate AllowedValues if present
        if (field.AllowedValues is { Count: 0 })
        {
            errors.Add($"{routeIdentifier} -> {fieldIdentifier}: 'AllowedValues' is defined but empty. Either remove it or add values.");
        }
    }
}