using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Pedidos.Infrastructure.Persistence;

/// <summary>
/// Usada solo por las herramientas de EF (dotnet ef migrations add ...).
/// La cadena real en runtime viene de appsettings / variables de entorno.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=localhost,1433;Database=PedidosDb;User Id=sa;Password=Pedidos_Str0ng!Pass;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AppDbContext(options, TimeProvider.System);
    }
}