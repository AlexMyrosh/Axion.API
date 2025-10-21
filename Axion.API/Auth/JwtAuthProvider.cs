using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Axion.API.Auth;

public class JwtAuthProvider(IConfiguration config) : IAuthProvider
{
    public bool Validate(string token)
    {
        var jwtSection = config.GetSection("Jwt");
        var key = jwtSection.GetValue<string>("Secret");
        if (string.IsNullOrEmpty(key))
        {
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
        catch
        {
            return false;
        }
    }
}