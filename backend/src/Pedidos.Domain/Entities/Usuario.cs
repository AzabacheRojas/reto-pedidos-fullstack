using Pedidos.Domain.Common;
using Pedidos.Domain.Exceptions;

namespace Pedidos.Domain.Entities;

public sealed class Usuario : AuditableEntity
{
    public string Email { get; private set; } = string.Empty;
    public string Nombre { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Rol { get; private set; } = Roles.User;
    public bool Activo { get; private set; } = true;

    private Usuario() { }

    public static Usuario Crear(string email, string nombre, string passwordHash, string rol)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("El email es obligatorio.");
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new DomainException("La contraseña es obligatoria.");
        if (!Roles.EsValido(rol)) throw new DomainException($"Rol inválido: {rol}.");

        return new Usuario
        {
            Email = NormalizarEmail(email),
            Nombre = nombre.Trim(),
            PasswordHash = passwordHash,
            Rol = rol,
            Activo = true
        };
    }

    public static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";

    public static bool EsValido(string rol) => rol is Admin or User;
}