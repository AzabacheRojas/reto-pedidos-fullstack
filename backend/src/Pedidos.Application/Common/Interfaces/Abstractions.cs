using Pedidos.Application.Pedidos;
using Pedidos.Domain.Entities;

namespace Pedidos.Application.Common.Interfaces;

// Puertos (Dependency Inversion): la capa de aplicación define los contratos
// y la infraestructura los implementa. Application no conoce EF Core ni JWT.

public interface IPedidoRepository
{
    Task<Pedido?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Verifica unicidad del número (incluye pedidos eliminados lógicamente).</summary>
    Task<bool> ExistsNumeroAsync(string numeroPedido, int? excludeId = null, CancellationToken ct = default);

    Task<(IReadOnlyList<Pedido> Items, int TotalCount)> GetPagedAsync(PedidoQuery query, CancellationToken ct = default);

    void Add(Pedido pedido);
}

public interface IUsuarioRepository
{
    Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct = default);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

public interface IJwtTokenGenerator
{
    TokenResult Generate(Usuario usuario);
}

public sealed record TokenResult(string Token, int ExpiresIn);