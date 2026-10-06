using Microsoft.EntityFrameworkCore;
using Pedidos.Application.Common.Interfaces;
using Pedidos.Application.Pedidos;
using Pedidos.Domain.Entities;
using Pedidos.Domain.Enums;

namespace Pedidos.Infrastructure.Persistence.Repositories;

internal sealed class PedidoRepository(AppDbContext context) : IPedidoRepository
{
    public Task<Pedido?> GetByIdAsync(int id, CancellationToken ct = default) =>
        context.Pedidos.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> ExistsNumeroAsync(string numeroPedido, int? excludeId = null, CancellationToken ct = default) =>
        context.Pedidos
            .IgnoreQueryFilters() // el número es único incluso respecto a pedidos eliminados
            .AnyAsync(p => p.NumeroPedido == numeroPedido && (excludeId == null || p.Id != excludeId), ct);

    public async Task<(IReadOnlyList<Pedido> Items, int TotalCount)> GetPagedAsync(
        PedidoQuery query, CancellationToken ct = default)
    {
        IQueryable<Pedido> q = context.Pedidos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(p => p.NumeroPedido.Contains(term) || p.Cliente.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(query.Estado) &&
            Enum.TryParse<EstadoPedido>(query.Estado, ignoreCase: true, out var estado))
        {
            q = q.Where(p => p.Estado == estado);
        }

        var total = await q.CountAsync(ct);

        var items = await ApplySort(q, query.SortBy, query.Desc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public void Add(Pedido pedido) => context.Pedidos.Add(pedido);

    private static IQueryable<Pedido> ApplySort(IQueryable<Pedido> q, string? sortBy, bool desc)
    {
        IOrderedQueryable<Pedido> ordered = sortBy switch
        {
            "numeropedido" => desc ? q.OrderByDescending(p => p.NumeroPedido) : q.OrderBy(p => p.NumeroPedido),
            "cliente" => desc ? q.OrderByDescending(p => p.Cliente) : q.OrderBy(p => p.Cliente),
            "total" => desc ? q.OrderByDescending(p => p.Total) : q.OrderBy(p => p.Total),
            "estado" => desc ? q.OrderByDescending(p => p.Estado) : q.OrderBy(p => p.Estado),
            _ => desc ? q.OrderByDescending(p => p.Fecha) : q.OrderBy(p => p.Fecha)
        };

        // Desempate estable para que la paginación sea determinista.
        return desc ? ordered.ThenByDescending(p => p.Id) : ordered.ThenBy(p => p.Id);
    }
}