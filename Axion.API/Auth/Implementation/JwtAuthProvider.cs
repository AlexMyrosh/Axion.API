using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text.Json;
using Axion.API.Auth.Abstraction;
using Microsoft.IdentityModel.Tokens;

namespace Axion.API.Auth.Implementation;

public class JwtAuthProvider(IConfiguration config, ILogger<JwtAuthProvider> logger) : IJwtAuthProvider
{
    public bool Validate(string token, JsonElement? body)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogWarning("JWT token is null or empty");
            return false;
        }
        
        var publicKey = config["Jwt:PublicKey"];
        if (string.IsNullOrEmpty(publicKey))
        {
            logger.LogError("JWT PublicKey is not found");
            return false;
        }

        try
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(publicKey);
            var rsaSecurityKey = new RsaSecurityKey(rsa);
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParams = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = rsaSecurityKey,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                ClockSkew = TimeSpan.FromMinutes(1)
            };
            
            tokenHandler.ValidateToken(token, validationParams, out var validatedToken);
            if (validatedToken is not JwtSecurityToken jwtToken)
            {
                logger.LogWarning("Failed to parse JWT token");
                return false;
            }

            // Check for timestamp in parsed JWT token
            var timestampClaim = jwtToken.Claims.FirstOrDefault(c => c.Type.Equals("timestamp", StringComparison.OrdinalIgnoreCase));
            if (timestampClaim == null)
            {
                logger.LogWarning("JWT token does not contain 'timestamp' claim");
                return false;
            }
            
            if (body is { ValueKind: JsonValueKind.Object })
            {
                // Take payload part from JWT
                var jwtPayloadJson = jwtToken.Payload.SerializeToJson();
                var jwtPayload = JsonDocument.Parse(jwtPayloadJson).RootElement;

                // Compare JWT payload with body request (excluding timestamp and regular JWT claims)
                if (!CompareJsonObjects(jwtPayload, body.Value))
                {
                    logger.LogWarning("Request body does not match JWT token payload");
                    return false;
                }
            }

            logger.LogInformation("JWT token validated successfully");
            return true;
        }
        catch (SecurityTokenExpiredException ex)
        {
            logger.LogWarning(ex, "JWT token has expired");
            return false;
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            logger.LogWarning(ex, "JWT token has invalid signature");
            return false;
        }
        catch (CryptographicException ex)
        {
            logger.LogError(ex, "Failed to parse public key");
            return false;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "JWT token validation failed");
            return false;
        }
    }
    
    private bool CompareJsonObjects(JsonElement jwtPayload, JsonElement requestBody)
    {
        // Claims to ignore during comparison (standard JWT claims + timestamp)
        var ignoredFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "timestamp", "iat", "exp", "nbf", "iss", "aud", "sub", "jti"
        };
        
        var bodyProperties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in requestBody.EnumerateObject().Where(prop => !ignoredFields.Contains(prop.Name)))
        {
            bodyProperties[prop.Name] = prop.Value;
        }
        
        var jwtProperties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in jwtPayload.EnumerateObject().Where(prop => !ignoredFields.Contains(prop.Name)))
        {
            jwtProperties[prop.Name] = prop.Value;
        }
        
        if (bodyProperties.Count != jwtProperties.Count)
        {
            logger.LogWarning("Field count mismatch: JWT has {JwtCount} fields, body has {BodyCount} fields (excluding ignored fields)", jwtProperties.Count, bodyProperties.Count);
            return false;
        }
        
        foreach (var bodyProp in bodyProperties)
        {
            if (!jwtProperties.TryGetValue(bodyProp.Key, out var jwtValue))
            {
                logger.LogWarning("Field '{FieldName}' exists in body but not in JWT token", bodyProp.Key);
                return false;
            }

            if (!JsonElementsEqual(bodyProp.Value, jwtValue))
            {
                logger.LogWarning("Field '{FieldName}' value mismatch: JWT='{JwtValue}', Body='{BodyValue}'", bodyProp.Key, jwtValue, bodyProp.Value);
                return false;
            }
        }

        logger.LogDebug("JWT payload and request body match successfully");
        return true;
    }
    
    private static bool JsonElementsEqual(JsonElement element1, JsonElement element2)
    {
        if (element1.ValueKind != element2.ValueKind)
        {
            return false;
        }

        switch (element1.ValueKind)
        {
            case JsonValueKind.Object:
                var props1 = element1.EnumerateObject().OrderBy(p => p.Name).ToList();
                var props2 = element2.EnumerateObject().OrderBy(p => p.Name).ToList();

                if (props1.Count != props2.Count)
                {
                    return false;
                }
                
                for (var i = 0; i < props1.Count; i++)
                {
                    if (props1[i].Name != props2[i].Name)
                    {
                        return false;
                    }

                    if (!JsonElementsEqual(props1[i].Value, props2[i].Value))
                    {
                        return false;
                    }
                }
                
                return true;

            case JsonValueKind.Array:
                var arr1 = element1.EnumerateArray().ToList();
                var arr2 = element2.EnumerateArray().ToList();

                if (arr1.Count != arr2.Count)
                {
                    return false;
                }
                
                for (var i = 0; i < arr1.Count; i++)
                {
                    if (!JsonElementsEqual(arr1[i], arr2[i]))
                    {
                        return false;
                    }
                }
                
                return true;

            case JsonValueKind.String:
                return element1.GetString() == element2.GetString();

            case JsonValueKind.Number:
                return element1.GetRawText() == element2.GetRawText();

            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return true;
            
            default:
                return element1.GetRawText() == element2.GetRawText();
        }
    }
}
