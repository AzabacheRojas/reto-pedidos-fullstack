namespace Pedidos.Domain.Exceptions;

/// <summary>Violación de una regla de negocio (invariante) del dominio.</summary>
public sealed class DomainException(string message) : Exception(message)
{
}