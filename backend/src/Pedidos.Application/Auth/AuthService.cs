using FluentValidation;
using Microsoft.Extensions.Logging;
using Pedidos.Application.Common;
using Pedidos.Application.Common.Exceptions;
using Pedidos.Application.Common.Interfaces;
using Pedidos.Domain.Entities;

namespace Pedidos.Application.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record UsuarioDto(string Email, string Nombre, string Rol);

public sealed record LoginResponse(string Token, int ExpiresIn, UsuarioDto Usuario);

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .EmailAddress().WithMessage("El email no tiene un formato válido.")
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MaximumLength(128);
    }
}

internal sealed class AuthService(
    IUsuarioRepository usuarios,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator tokenGenerator,
    IValidator<LoginRequest> validator,
    ILogger<AuthService> logger) : IAuthService
{
    // Mensaje genérico: no revelamos si el email existe (evita enumeración de usuarios).
    private const string CredencialesInvalidas = "Credenciales inválidas.";

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        await validator.EnsureValidAsync(request, ct);

        var email = Usuario.NormalizarEmail(request.Email);
        var usuario = await usuarios.GetByEmailAsync(email, ct);

        if (usuario is null || !usuario.Activo || !passwordHasher.Verify(request.Password, usuario.PasswordHash))
        {
            logger.LogWarning("Intento de login fallido para {Email}", email);
            throw new UnauthorizedException(CredencialesInvalidas);
        }

        var token = tokenGenerator.Generate(usuario);
        logger.LogInformation("Login exitoso para {Email} con rol {Rol}", usuario.Email, usuario.Rol);

        return new LoginResponse(token.Token, token.ExpiresIn, new UsuarioDto(usuario.Email, usuario.Nombre, usuario.Rol));
    }
}