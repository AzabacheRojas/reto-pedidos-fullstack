using Microsoft.EntityFrameworkCore;
using Pedidos.Application.Common.Interfaces;
using Pedidos.Domain.Entities;

namespace Pedidos.Infrastructure.Persistence.Repositories;

internal sealed class UsuarioRepository(AppDbContext context) : IUsuarioRepository
{
    public Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);
}