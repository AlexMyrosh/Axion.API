using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Axion.API.Utilities;

public class JwtTokenGenerator(IConfiguration configuration)
{
    public string Generate(object payload)
    {
        try
        {
            var privateKey = configuration["Jwt:PrivateKey"] ?? throw new InvalidOperationException("JWT PrivateKey is not configured");
            var issuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured");
            var audience = configuration["Jwt:Audience"]  ?? throw new InvalidOperationException("JWT Audience is not configured");
            var expirationMinutes = configuration["Jwt:ExpirationMinutes"]  ?? throw new InvalidOperationException("JWT ExpirationMinutes is not configured");

            var rsa = RSA.Create();
            rsa.ImportFromPem(privateKey);
            var signingCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);

            var payloadJson = JsonSerializer.Serialize(payload);
            var payloadDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadJson);
            var claims = payloadDict?.Select(kvp => new Claim(kvp.Key, GetClaimValue(kvp.Value))).ToList() ?? [];

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(int.Parse(expirationMinutes)),
                SigningCredentials = signingCredentials,
                Issuer = issuer,
                Audience = audience
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to generate JWT token: {ex.Message}", ex);
        }
    }

    private static string GetClaimValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString() ?? string.Empty,
        JsonValueKind.Number => value.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null => string.Empty,
        _ => value.GetRawText()
    };
}