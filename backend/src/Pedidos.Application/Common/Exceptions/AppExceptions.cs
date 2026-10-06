namespace Pedidos.Application.Common.Exceptions;

/// <summary>Excepciones de aplicación. El manejador global las traduce a ProblemDetails (RFC 7807).</summary>
public abstract class AppException(string message) : Exception(message)
{
}

public sealed class NotFoundException(string recurso, object clave)
    : AppException($"{recurso} con identificador '{clave}' no fue encontrado.")
{
}

public sealed class ConflictException(string message) : AppException(message)
{
}

public sealed class UnauthorizedException(string message) : AppException(message)
{
}

public sealed class ValidationException : AppException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("Se produjeron uno o más errores de validación.")
    {
        Errors = errors;
    }
}