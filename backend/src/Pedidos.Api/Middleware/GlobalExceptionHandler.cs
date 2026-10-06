using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Pedidos.Application.Common.Exceptions;
using Pedidos.Domain.Exceptions;

namespace Pedidos.Api.Middleware;

/// <summary>
/// Manejo global de excepciones: traduce excepciones de dominio/aplicación a respuestas
/// ProblemDetails (RFC 7807) consistentes y nunca expone detalles internos en errores 500.
/// </summary>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var problem = exception switch
        {
            ValidationException ex => new ValidationProblemDetails(ex.Errors.ToDictionary(k => k.Key, v => v.Value))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Error de validación",
                Detail = ex.Message
            },
            DomainException ex => Problem(StatusCodes.Status400BadRequest, "Regla de negocio incumplida", ex.Message),
            NotFoundException ex => Problem(StatusCodes.Status404NotFound, "Recurso no encontrado", ex.Message),
            ConflictException ex => Problem(StatusCodes.Status409Conflict, "Conflicto", ex.Message),
            UnauthorizedException ex => Problem(StatusCodes.Status401Unauthorized, "No autorizado", ex.Message),
            BadHttpRequestException ex => Problem(StatusCodes.Status400BadRequest, "Solicitud inválida", ex.Message),
            _ => Problem(StatusCodes.Status500InternalServerError, "Error interno del servidor",
                "Ocurrió un error inesperado. Intente nuevamente más tarde.")
        };

        if (problem.Status >= 500)
            logger.LogError(exception, "Error no controlado procesando {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        else
            logger.LogWarning("Solicitud rechazada ({Status}) en {Method} {Path}: {Message}",
                problem.Status, httpContext.Request.Method, httpContext.Request.Path, exception.Message);

        problem.Instance = httpContext.Request.Path;
        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private static ProblemDetails Problem(int status, string title, string detail) =>
        new() { Status = status, Title = title, Detail = detail };
}