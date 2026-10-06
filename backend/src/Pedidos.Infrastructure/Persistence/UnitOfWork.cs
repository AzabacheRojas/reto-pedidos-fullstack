using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Pedidos.Application.Common.Exceptions;
using Pedidos.Application.Common.Interfaces;

namespace Pedidos.Infrastructure.Persistence;

internal sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    // 2601: duplicado en índice único · 2627: violación de constraint UNIQUE/PK
    private static readonly int[] UniqueViolationCodes = [2601, 2627];

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && UniqueViolationCodes.Contains(sql.Number))
        {
            // Condición de carrera: otro request insertó el mismo número entre la validación y el guardado.
            throw new ConflictException("Ya existe un registro con el mismo valor único (número de pedido).");
        }
    }
}