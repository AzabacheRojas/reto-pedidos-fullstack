using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pedidos.Domain.Entities;

namespace Pedidos.Infrastructure.Persistence.Configurations;

internal sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).UseIdentityColumn();

        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("UX_Usuarios_Email");

        builder.Property(u => u.Nombre).HasMaxLength(150).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(200).IsRequired();
        builder.Property(u => u.Rol).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(u => u.Activo).IsRequired();
        builder.Property(u => u.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(u => u.UpdatedAt).HasColumnType("datetime2");
    }
}