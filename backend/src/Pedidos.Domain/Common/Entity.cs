namespace Pedidos.Domain.Common;

/// <summary>Entidad base con identificador.</summary>
public abstract class Entity
{
    public int Id { get; protected set; }
}

/// <summary>Entidad con campos de auditoría, completados por la capa de persistencia.</summary>
public abstract class AuditableEntity : Entity
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Marca entidades que soportan eliminación lógica.</summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }
}