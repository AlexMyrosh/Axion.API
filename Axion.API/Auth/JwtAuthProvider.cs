using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Axion.API.Auth;

public class JwtAuthProvider(IConfiguration config, ILogger<JwtAuthProvider> logger) : IAuthProvider
{
    public bool Validate(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogWarning("JWT token is null or empty");
            return false;
        }

        var jwtSection = config.GetSection("Jwt");
        var key = jwtSection.GetValue<string>("Secret");
        if (string.IsNullOrEmpty(key))
        {
            logger.LogError("JWT Secret is not configured");
            return false;
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        try
        {
            tokenHandler.ValidateToken(token, validationParams, out _);
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
        catch (Exception ex)
        {
            logger.LogWarning(ex, "JWT token validation failed");
            return false;
        }
    }
}