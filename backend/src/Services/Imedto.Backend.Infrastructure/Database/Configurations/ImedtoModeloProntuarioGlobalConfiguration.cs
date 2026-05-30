using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Admin;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class ImedtoModeloProntuarioGlobalConfiguration : IEntityTypeConfiguration<ImedtoModeloProntuarioGlobal>
{
    public void Configure(EntityTypeBuilder<ImedtoModeloProntuarioGlobal> builder)
    {
        builder.ToTable("imedto_modelo_prontuario_global");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(m => m.Nome).HasColumnName("nome").HasColumnType("text").IsRequired();
        builder.Property(m => m.Descricao).HasColumnName("descricao").HasColumnType("text");
        builder.Property(m => m.ConteudoJson)
            .HasColumnName("conteudo_json")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasDefaultValueSql("'{}'::jsonb");
        builder.Property(m => m.Ativo).HasColumnName("ativo").IsRequired().HasDefaultValue(true);
        builder.Property(m => m.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(m => m.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(m => m.CriadoPorAdminId).HasColumnName("criado_por_admin_id");
        builder.Property(m => m.AtualizadoPorAdminId).HasColumnName("atualizado_por_admin_id");

        // FKs para admin — nullable, SET NULL ao excluir admin.
        builder.HasOne<ImedtoAdmin>()
            .WithMany()
            .HasForeignKey(m => m.CriadoPorAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<ImedtoAdmin>()
            .WithMany()
            .HasForeignKey(m => m.AtualizadoPorAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        // Unique case-insensitive no nome (via índice expressional — ver arquivo _indices.sql para CONCURRENTLY).
        // O índice é criado em arquivo separado por ser CONCURRENTLY.
        builder.HasIndex(m => m.Nome)
            .HasDatabaseName("uq_imedto_modelo_prontuario_global_nome_lower")
            .IsUnique();

        // Índice de listagem filtrada (ativo, nome).
        builder.HasIndex(m => new { m.Ativo, m.Nome })
            .HasDatabaseName("ix_imedto_modelo_prontuario_global_ativo_nome");

        builder.Ignore(m => m.DomainEvents);
    }
}
