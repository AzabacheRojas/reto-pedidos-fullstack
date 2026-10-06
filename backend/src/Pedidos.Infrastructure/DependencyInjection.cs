using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pedidos.Application.Common.Interfaces;
using Pedidos.Infrastructure.Persistence;
using Pedidos.Infrastructure.Persistence.Repositories;
using Pedidos.Infrastructure.Security;

namespace Pedidos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Falta la cadena de conexión 'DefaultConnection'.");

        services.TryAddSingleton(TimeProvider.System);

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                // Resiliencia nativa de EF Core: reintenta errores transitorios de SQL Server.
                sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
                sql.CommandTimeout(30);
                sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
            }));

        services.AddScoped<IPedidoRepository, PedidoRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

        return services;
    }
}