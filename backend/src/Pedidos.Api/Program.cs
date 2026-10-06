using Pedidos.Api.Extensions;
using Pedidos.Application;
using Pedidos.Infrastructure;
using Pedidos.Infrastructure.Persistence;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Logging estructurado (configurable desde appsettings → "Serilog").
    builder.Host.UseSerilog((context, services, config) => config
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Pedidos.Api"));

    // Composition root: cada capa registra sus propias dependencias.
    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration)
        .AddApiServices(builder.Configuration);

    var app = builder.Build();

    app.UseExceptionHandler();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
    {
        app.UseSwagger();
        app.UseSwaggerUI(o => o.DocumentTitle = "Pedidos API");
    }

    app.UseCors(ApiServiceExtensions.CorsPolicy);
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health").AllowAnonymous();
    app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous().ExcludeFromDescription();

    // Migraciones automáticas + datos semilla al iniciar.
    if (app.Configuration.GetValue("Database:MigrateOnStartup", true))
        await app.Services.InitializeDatabaseAsync();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "La aplicación terminó de forma inesperada");
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program { }