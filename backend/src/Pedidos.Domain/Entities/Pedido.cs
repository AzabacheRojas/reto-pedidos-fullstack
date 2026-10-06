using Pedidos.Domain.Common;
using Pedidos.Domain.Enums;
using Pedidos.Domain.Exceptions;

namespace Pedidos.Domain.Entities;

/// <summary>
/// Agregado Pedido. Protege sus invariantes: no se puede construir ni modificar
/// en un estado inválido (por ejemplo, con total menor o igual a 0).
/// </summary>
public sealed class Pedido : AuditableEntity, ISoftDeletable
{
    public const int NumeroPedidoMaxLength = 50;
    public const int ClienteMaxLength = 150;

    public string NumeroPedido { get; private set; } = string.Empty;
    public string Cliente { get; private set; } = string.Empty;
    public DateTime Fecha { get; private set; }
    public decimal Total { get; private set; }
    public EstadoPedido Estado { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    // Requerido por EF Core.
    private Pedido() { }

    public static Pedido Crear(
        string numeroPedido,
        string cliente,
        DateTime fecha,
        decimal total,
        EstadoPedido estado = EstadoPedido.Registrado)
    {
        var pedido = new Pedido();
        pedido.Aplicar(numeroPedido, cliente, fecha, total, estado);
        return pedido;
    }

    public void Actualizar(string numeroPedido, string cliente, DateTime fecha, decimal total, EstadoPedido estado)
    {
        if (IsDeleted)
            throw new DomainException("No se puede modificar un pedido eliminado.");

        Aplicar(numeroPedido, cliente, fecha, total, estado);
    }

    /// <summary>Eliminación lógica: el registro se conserva para trazabilidad.</summary>
    public void Eliminar(DateTime utcNow)
    {
        if (IsDeleted) return;

        IsDeleted = true;
        DeletedAt = utcNow;
    }

    public static string NormalizarNumero(string numeroPedido) =>
        (numeroPedido ?? string.Empty).Trim().ToUpperInvariant();

    private void Aplicar(string numeroPedido, string cliente, DateTime fecha, decimal total, EstadoPedido estado)
    {
        var numero = NormalizarNumero(numeroPedido);
        if (string.IsNullOrWhiteSpace(numero))
            throw new DomainException("El número de pedido es obligatorio.");
        if (numero.Length > NumeroPedidoMaxLength)
            throw new DomainException($"El número de pedido no puede superar {NumeroPedidoMaxLength} caracteres.");

        var nombreCliente = (cliente ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nombreCliente))
            throw new DomainException("El cliente es obligatorio.");
        if (nombreCliente.Length > ClienteMaxLength)
            throw new DomainException($"El cliente no puede superar {ClienteMaxLength} caracteres.");

        if (total <= 0)
            throw new DomainException("El total del pedido debe ser mayor a 0.");

        if (fecha == default)
            throw new DomainException("La fecha del pedido es obligatoria.");

        if (!Enum.IsDefined(estado))
            throw new DomainException("El estado del pedido no es válido.");

        NumeroPedido = numero;
        Cliente = nombreCliente;
        Fecha = fecha;
        Total = decimal.Round(total, 2, MidpointRounding.AwayFromZero);
        Estado = estado;
    }
}