using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Myrati.Application.Abstractions;
using Myrati.Domain.Identity;

namespace Myrati.Infrastructure.Security;

public sealed class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    public AccessTokenResult GenerateAccessToken(AdminUser user)
    {
        var issuer = configuration["Jwt:Issuer"] ?? "Myrati";
        var audience = configuration["Jwt:Audience"] ?? "Myrati.Backoffice";
        var signingKey = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key não configurada.");
        var expiresInMinutes = int.TryParse(configuration["Jwt:ExpiresInMinutes"], out var parsedExpiresInMinutes)
            ? parsedExpiresInMinutes
            : 480;

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(expiresInMinutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AccessTokenResult(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt);
    }
}
