using FluentValidation;
using Microsoft.Extensions.Logging;
using Pedidos.Application.Common;
using Pedidos.Application.Common.Exceptions;
using Pedidos.Application.Common.Interfaces;
using Pedidos.Application.Common.Models;
using Pedidos.Domain.Entities;
using Pedidos.Domain.Enums;
using ValidationException = Pedidos.Application.Common.Exceptions.ValidationException;

namespace Pedidos.Application.Pedidos;

public interface IPedidoService
{
    Task<PagedResult<PedidoDto>> GetAllAsync(PedidoQuery query, CancellationToken ct = default);
    Task<PedidoDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PedidoDto> CreateAsync(CrearPedidoRequest request, CancellationToken ct = default);
    Task<PedidoDto> UpdateAsync(int id, ActualizarPedidoRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

internal sealed class PedidoService(
    IPedidoRepository repository,
    IUnitOfWork unitOfWork,
    IValidator<CrearPedidoRequest> crearValidator,
    IValidator<ActualizarPedidoRequest> actualizarValidator,
    TimeProvider timeProvider,
    ILogger<PedidoService> logger) : IPedidoService
{
    private const string Recurso = "Pedido";

    public async Task<PagedResult<PedidoDto>> GetAllAsync(PedidoQuery query, CancellationToken ct = default)
    {
        var q = query.Normalizada();
        if (q.Estado is not null && !PedidoRequestValidator<CrearPedidoRequest>.EstadoValido(q.Estado))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["estado"] = [$"Estado inválido. Valores permitidos: {string.Join(", ", Enum.GetNames<EstadoPedido>())}."]
            });
        }

        var (items, total) = await repository.GetPagedAsync(q, ct);
        return new PagedResult<PedidoDto>(items.Select(p => p.ToDto()).ToList(), q.Page, q.PageSize, total);
    }

    public async Task<PedidoDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var pedido = await repository.GetByIdAsync(id, ct) ?? throw new NotFoundException(Recurso, id);
        return pedido.ToDto();
    }

    public async Task<PedidoDto> CreateAsync(CrearPedidoRequest request, CancellationToken ct = default)
    {
        await crearValidator.EnsureValidAsync(request, ct);

        var numero = Pedido.NormalizarNumero(request.NumeroPedido);
        await EnsureNumeroUnicoAsync(numero, excludeId: null, ct);

        var pedido = Pedido.Crear(numero, request.Cliente, request.Fecha, request.Total, ParseEstado(request.Estado));

        repository.Add(pedido);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Pedido {PedidoId} ({NumeroPedido}) creado", pedido.Id, pedido.NumeroPedido);
        return pedido.ToDto();
    }

    public async Task<PedidoDto> UpdateAsync(int id, ActualizarPedidoRequest request, CancellationToken ct = default)
    {
        await actualizarValidator.EnsureValidAsync(request, ct);

        var pedido = await repository.GetByIdAsync(id, ct) ?? throw new NotFoundException(Recurso, id);

        var numero = Pedido.NormalizarNumero(request.NumeroPedido);
        if (!string.Equals(numero, pedido.NumeroPedido, StringComparison.Ordinal))
            await EnsureNumeroUnicoAsync(numero, excludeId: id, ct);

        pedido.Actualizar(numero, request.Cliente, request.Fecha, request.Total, ParseEstado(request.Estado));
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Pedido {PedidoId} actualizado", id);
        return pedido.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var pedido = await repository.GetByIdAsync(id, ct) ?? throw new NotFoundException(Recurso, id);

        pedido.Eliminar(timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Pedido {PedidoId} eliminado (lógico)", id);
    }

    private async Task EnsureNumeroUnicoAsync(string numero, int? excludeId, CancellationToken ct)
    {
        if (await repository.ExistsNumeroAsync(numero, excludeId, ct))
            throw new ConflictException($"Ya existe un pedido con el número '{numero}'.");
    }

    private static EstadoPedido ParseEstado(string? estado) =>
        string.IsNullOrWhiteSpace(estado)
            ? EstadoPedido.Registrado
            : Enum.Parse<EstadoPedido>(estado, ignoreCase: true);
}