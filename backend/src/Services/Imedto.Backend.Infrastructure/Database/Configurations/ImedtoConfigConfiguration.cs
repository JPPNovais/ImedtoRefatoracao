using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Admin;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class ImedtoConfigConfiguration : IEntityTypeConfiguration<ImedtoConfig>
{
    public void Configure(EntityTypeBuilder<ImedtoConfig> builder)
    {
        builder.ToTable("imedto_config");

        // PK é a chave (string), não gerada automaticamente.
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("chave").ValueGeneratedNever().IsRequired();

        builder.Property(c => c.Valor).HasColumnName("valor").HasColumnType("jsonb").IsRequired();
        builder.Property(c => c.Descricao).HasColumnName("descricao");
        builder.Property(c => c.AtualizadoEm).HasColumnName("atualizado_em").IsRequired();
        builder.Property(c => c.AtualizadoPorAdminId).HasColumnName("atualizado_por_admin_id");

        // FK para admin que fez a última atualização (nullable — seed inicial pode não ter admin).
        builder.HasOne<ImedtoAdmin>()
            .WithMany()
            .HasForeignKey(c => c.AtualizadoPorAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Ignore(c => c.DomainEvents);
    }
}
