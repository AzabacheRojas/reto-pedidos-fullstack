using FluentValidation;
using Pedidos.Domain.Entities;
using Pedidos.Domain.Enums;

namespace Pedidos.Application.Pedidos;

/// <summary>
/// Validación de entrada (formato). Las invariantes de negocio se protegen además en
/// la entidad de dominio (defensa en profundidad), y la unicidad en el servicio y en la BD.
/// </summary>
public abstract class PedidoRequestValidator<T> : AbstractValidator<T> where T : IPedidoRequest
{
    protected PedidoRequestValidator()
    {
        RuleFor(x => x.NumeroPedido)
            .NotEmpty().WithMessage("El número de pedido es obligatorio.")
            .MaximumLength(Pedido.NumeroPedidoMaxLength)
                .WithMessage($"El número de pedido no puede superar {Pedido.NumeroPedidoMaxLength} caracteres.")
            .Matches("^[A-Za-z0-9-]+$")
                .WithMessage("El número de pedido solo admite letras, números y guiones (ej: PED-001).");

        RuleFor(x => x.Cliente)
            .NotEmpty().WithMessage("El cliente es obligatorio.")
            .MaximumLength(Pedido.ClienteMaxLength)
                .WithMessage($"El cliente no puede superar {Pedido.ClienteMaxLength} caracteres.");

        RuleFor(x => x.Fecha)
            .NotEqual(default(DateTime)).WithMessage("La fecha es obligatoria.")
            .Must(f => f.Year >= 2000 && f <= DateTime.UtcNow.AddYears(1))
                .WithMessage("La fecha no es válida.");

        RuleFor(x => x.Total)
            .GreaterThan(0).WithMessage("El total debe ser mayor a 0.")
            .PrecisionScale(10, 2, ignoreTrailingZeros: true)
                .WithMessage("El total admite como máximo 8 enteros y 2 decimales.");

        RuleFor(x => x.Estado)
            .Must(EstadoValido)
            .When(x => !string.IsNullOrWhiteSpace(x.Estado))
            .WithMessage($"Estado inválido. Valores permitidos: {string.Join(", ", Enum.GetNames<EstadoPedido>())}.");
    }

    /// <summary>Acepta solo nombres del enum (no valores numéricos como "1").</summary>
    internal static bool EstadoValido(string? estado) =>
        !string.IsNullOrWhiteSpace(estado)
        && !int.TryParse(estado, out _)
        && Enum.TryParse<EstadoPedido>(estado, ignoreCase: true, out var e)
        && Enum.IsDefined(e);
}

public sealed class CrearPedidoRequestValidator : PedidoRequestValidator<CrearPedidoRequest>
{
}

public sealed class ActualizarPedidoRequestValidator : PedidoRequestValidator<ActualizarPedidoRequest>
{
    public ActualizarPedidoRequestValidator()
    {
        RuleFor(x => x.Estado).NotEmpty().WithMessage("El estado es obligatorio.");
    }
}