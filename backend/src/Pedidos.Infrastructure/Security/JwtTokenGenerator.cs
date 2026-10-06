using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Pedidos.Application.Common.Interfaces;
using Pedidos.Domain.Entities;

namespace Pedidos.Infrastructure.Security;

internal sealed class JwtTokenGenerator(IOptions<JwtSettings> options, TimeProvider timeProvider) : IJwtTokenGenerator
{
    private readonly JwtSettings _settings = options.Value;
    private static readonly JsonWebTokenHandler Handler = new();

    public TokenResult Generate(Usuario usuario)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(_settings.ExpirationMinutes);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtClaims.Subject, usuario.Id.ToString()),
                new Claim(JwtClaims.Email, usuario.Email),
                new Claim(JwtClaims.Name, usuario.Nombre),
                new Claim(JwtClaims.Role, usuario.Rol),
                new Claim(JwtClaims.JwtId, Guid.NewGuid().ToString())
            })
        };

        return new TokenResult(Handler.CreateToken(descriptor), _settings.ExpirationMinutes * 60);
    }
}

/// <summary>Nombres cortos de claims (estándar JWT) compartidos entre emisión y validación.</summary>
public static class JwtClaims
{
    public const string Subject = "sub";
    public const string Email = "email";
    public const string Name = "name";
    public const string Role = "role";
    public const string JwtId = "jti";
}