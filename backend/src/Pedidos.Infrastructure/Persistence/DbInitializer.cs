using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pedidos.Application.Common.Interfaces;
using Pedidos.Domain.Entities;
using Pedidos.Domain.Enums;
using Polly;
using Polly.Retry;

namespace Pedidos.Infrastructure.Persistence;

public static class DbInitializer
{
    /// <summary>
    /// Aplica migraciones pendientes y carga datos semilla.
    /// Usa un retry con backoff exponencial (Polly) porque, al levantar con Docker,
    /// SQL Server puede tardar varios segundos en aceptar conexiones.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");
        var context = sp.GetRequiredService<AppDbContext>();
        var hasher = sp.GetRequiredService<IPasswordHasher>();

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                // SqlException, RetryLimitExceededException de EF, timeouts, etc. (todo menos cancelación)
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => ex is not OperationCanceledException),
                MaxRetryAttempts = 8,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                MaxDelay = TimeSpan.FromSeconds(30),
                UseJitter = true,
                OnRetry = args =>
                {
                    logger.LogWarning(args.Outcome.Exception,
                        "Base de datos no disponible. Reintento {Attempt} en {Delay}s",
                        args.AttemptNumber + 1, args.RetryDelay.TotalSeconds);
                    return default;
                }
            })
            .Build();

        await pipeline.ExecuteAsync(async token =>
        {
            logger.LogInformation("Aplicando migraciones...");
            await context.Database.MigrateAsync(token);
        }, ct);

        await SeedAsync(context, hasher, logger, ct);
    }

    private static async Task SeedAsync(AppDbContext context, IPasswordHasher hasher, ILogger logger, CancellationToken ct)
    {
        if (!await context.Usuarios.AnyAsync(ct))
        {
            context.Usuarios.AddRange(
                Usuario.Crear("admin@pedidos.com", "Administrador", hasher.Hash("Admin123*"), Roles.Admin),
                Usuario.Crear("user@pedidos.com", "Usuario Demo", hasher.Hash("User123*"), Roles.User));

            await context.SaveChangesAsync(ct);
            logger.LogInformation("Usuarios semilla creados (admin@pedidos.com / user@pedidos.com)");
        }

        if (!await context.Pedidos.IgnoreQueryFilters().AnyAsync(ct))
        {
            var hoy = DateTime.UtcNow.Date;
            context.Pedidos.AddRange(
                Pedido.Crear("PED-001", "Juan Pérez", hoy.AddDays(-12), 250.75m),
                Pedido.Crear("PED-002", "María García", hoy.AddDays(-9), 1340.00m, EstadoPedido.EnProceso),
                Pedido.Crear("PED-003", "Carlos Rodríguez", hoy.AddDays(-7), 89.90m, EstadoPedido.Despachado),
                Pedido.Crear("PED-004", "Ana Torres", hoy.AddDays(-5), 560.40m, EstadoPedido.Entregado),
                Pedido.Crear("PED-005", "Luis Fernández", hoy.AddDays(-3), 120.00m, EstadoPedido.Cancelado),
                Pedido.Crear("PED-006", "Sofía Ramírez", hoy.AddDays(-1), 799.99m),
                Pedido.Crear("PED-007", "Distribuidora Andina S.A.", hoy, 4520.30m, EstadoPedido.EnProceso));

            await context.SaveChangesAsync(ct);
            logger.LogInformation("Pedidos de ejemplo creados");
        }
    }
}