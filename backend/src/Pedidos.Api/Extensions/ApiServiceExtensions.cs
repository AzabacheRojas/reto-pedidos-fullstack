using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Pedidos.Api.Middleware;
using Pedidos.Domain.Entities;
using Pedidos.Infrastructure.Security;

namespace Pedidos.Api.Extensions;

public static class AuthPolicies
{
    public const string AdminOnly = "AdminOnly";
}

public static class RateLimitPolicies
{
    public const string Login = "login";
}

public static class ApiServiceExtensions
{
    public const string CorsPolicy = "Frontend";

    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        // La validación de entrada la hace FluentValidation (mensajes de negocio en español),
        // por eso se desactiva el [Required] implícito de los tipos de referencia no anulables.
        services.AddControllers(o => o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
            .AddJsonOptions(o => o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddJwtAuthentication(configuration);
        services.AddRateLimiting();
        services.AddFrontendCors(configuration);
        services.AddSwaggerWithJwt();

        return services;
    }

    private static void AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("Falta la sección de configuración 'Jwt'.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtClaims.Name,
                    RoleClaimType = JwtClaims.Role
                };

                // Respuestas 401/403 en formato ProblemDetails, igual que el resto de errores.
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        var expired = context.AuthenticateFailure is SecurityTokenExpiredException;
                        if (expired) context.Response.Headers.Append("X-Token-Expired", "true");

                        await WriteProblemAsync(context.HttpContext, StatusCodes.Status401Unauthorized, "No autorizado",
                            expired ? "El token ha expirado. Inicie sesión nuevamente." : "Se requiere un token de acceso válido.");
                    },
                    OnForbidden = context => WriteProblemAsync(context.HttpContext, StatusCodes.Status403Forbidden,
                        "Acceso denegado", "No tiene permisos para realizar esta acción.")
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthPolicies.AdminOnly, p => p.RequireRole(Roles.Admin))
            // Seguro por defecto: cualquier endpoint nuevo exige autenticación salvo [AllowAnonymous].
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
    }

    private static async Task WriteProblemAsync(HttpContext httpContext, int status, string title, string detail)
    {
        httpContext.Response.StatusCode = status;
        var problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = httpContext.Request.Path
            }
        });
    }

    private static void AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Mitiga fuerza bruta: máx. 5 intentos de login por minuto por IP.
            options.AddPolicy(RateLimitPolicies.Login, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            options.OnRejected = async (context, ct) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();

                await WriteProblemAsync(context.HttpContext, StatusCodes.Status429TooManyRequests,
                    "Demasiadas solicitudes", "Ha superado el número de intentos permitidos. Espere un momento e intente de nuevo.");
            };
        });
    }

    private static void AddFrontendCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:5173" };

        services.AddCors(options => options.AddPolicy(CorsPolicy, policy => policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("X-Token-Expired", "Location")));
    }

    private static void AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Pedidos API",
                Version = "v1",
                Description = "API REST de gestión de pedidos — Reto técnico Fullstack Senior (.NET 8 + React)"
            });

            var scheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Ingrese el token JWT (sin el prefijo 'Bearer').",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = JwtBearerDefaults.AuthenticationScheme }
            };
            c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, scheme);
            c.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });

            var xml = Path.Combine(AppContext.BaseDirectory, $"{typeof(ApiServiceExtensions).Assembly.GetName().Name}.xml");
            if (File.Exists(xml)) c.IncludeXmlComments(xml);
        });
    }
}