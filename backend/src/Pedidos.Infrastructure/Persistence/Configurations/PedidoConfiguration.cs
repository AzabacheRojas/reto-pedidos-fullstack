using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pedidos.Domain.Entities;

namespace Pedidos.Infrastructure.Persistence.Configurations;

internal sealed class PedidoConfiguration : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        builder.ToTable("Pedidos", t =>
            t.HasCheckConstraint("CK_Pedidos_Total_Positivo", "[Total] > 0"));

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).UseIdentityColumn();

        builder.Property(p => p.NumeroPedido)
            .HasMaxLength(Pedido.NumeroPedidoMaxLength)
            .IsUnicode(false)
            .IsRequired();

        // Unicidad garantizada también a nivel de BD (cubre condiciones de carrera).
        builder.HasIndex(p => p.NumeroPedido)
            .IsUnique()
            .HasDatabaseName("UX_Pedidos_NumeroPedido");

        builder.Property(p => p.Cliente)
            .HasMaxLength(Pedido.ClienteMaxLength)
            .IsRequired();

        builder.Property(p => p.Fecha)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(p => p.Total)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(p => p.Estado)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(p => p.IsDeleted).IsRequired();
        builder.Property(p => p.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnType("datetime2");
        builder.Property(p => p.DeletedAt).HasColumnType("datetime2");

        builder.HasIndex(p => p.Fecha).HasDatabaseName("IX_Pedidos_Fecha");

        // Eliminación lógica: los pedidos borrados quedan fuera de todas las consultas por defecto.
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}