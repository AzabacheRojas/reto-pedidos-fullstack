using Pedidos.Domain.Entities;

namespace Pedidos.Application.Pedidos;

public sealed record PedidoDto(
    int Id,
    string NumeroPedido,
    string Cliente,
    DateTime Fecha,
    decimal Total,
    string Estado,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>Campos comunes a creación y edición (permite reutilizar el validador).</summary>
public interface IPedidoRequest
{
    string NumeroPedido { get; }
    string Cliente { get; }
    DateTime Fecha { get; }
    decimal Total { get; }
    string? Estado { get; }
}

public sealed record CrearPedidoRequest(
    string NumeroPedido,
    string Cliente,
    DateTime Fecha,
    decimal Total,
    string? Estado = null) : IPedidoRequest;

public sealed record ActualizarPedidoRequest(
    string NumeroPedido,
    string Cliente,
    DateTime Fecha,
    decimal Total,
    string? Estado) : IPedidoRequest;

/// <summary>Parámetros de búsqueda, filtrado, orden y paginación del listado.</summary>
public sealed class PedidoQuery
{
    public const int MaxPageSize = 100;

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    /// <summary>Busca por número de pedido o cliente.</summary>
    public string? Search { get; set; }

    public string? Estado { get; set; }

    /// <summary>numeroPedido | cliente | fecha | total | estado</summary>
    public string? SortBy { get; set; }

    public bool Desc { get; set; } = true;

    internal PedidoQuery Normalizada() => new()
    {
        Page = Math.Max(1, Page),
        PageSize = Math.Clamp(PageSize, 1, MaxPageSize),
        Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(),
        Estado = string.IsNullOrWhiteSpace(Estado) ? null : Estado.Trim(),
        SortBy = SortBy?.Trim().ToLowerInvariant(),
        Desc = Desc
    };
}

internal static class PedidoMappings
{
    public static PedidoDto ToDto(this Pedido p) =>
        new(p.Id, p.NumeroPedido, p.Cliente, p.Fecha, p.Total, p.Estado.ToString(), p.CreatedAt, p.UpdatedAt);
}