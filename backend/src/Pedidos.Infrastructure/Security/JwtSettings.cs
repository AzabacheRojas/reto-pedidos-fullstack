using System.ComponentModel.DataAnnotations;

namespace Pedidos.Infrastructure.Security;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    [Required] public string Issuer { get; init; } = string.Empty;
    [Required] public string Audience { get; init; } = string.Empty;

    /// <summary>Clave HMAC-SHA256. Mínimo 32 caracteres (256 bits). En producción debe venir de un secret store.</summary>
    [Required, MinLength(32)] public string SecretKey { get; init; } = string.Empty;

    [Range(1, 1440)] public int ExpirationMinutes { get; init; } = 60;
}