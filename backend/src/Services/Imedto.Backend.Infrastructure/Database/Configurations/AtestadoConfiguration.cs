using Imedto.Backend.Domain.Atestados;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class AtestadoConfiguration : IEntityTypeConfiguration<Atestado>
{
    public void Configure(EntityTypeBuilder<Atestado> builder)
    {
        builder.ToTable("atestados");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(a => a.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(a => a.PacienteId).HasColumnName("paciente_id").IsRequired();
        builder.Property(a => a.ProfissionalUsuarioId).HasColumnName("profissional_usuario_id").IsRequired();
        builder.Property(a => a.Tipo).HasColumnName("tipo").HasMaxLength(20).HasConversion<string>().IsRequired();
        builder.Property(a => a.DiasAfastamento).HasColumnName("dias_afastamento");
        builder.Property(a => a.Cid10).HasColumnName("cid10").HasMaxLength(8);
        builder.Property(a => a.Conteudo).HasColumnName("conteudo").HasColumnType("text").IsRequired();
        builder.Property(a => a.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(a => a.DeletadoEm).HasColumnName("deletado_em");
        builder.Property(a => a.DeletadoPorUsuarioId).HasColumnName("deletado_por_usuario_id");

        // Listagem por paciente — ordenada desc por criado_em.
        builder.HasIndex(a => new { a.PacienteId, a.CriadoEm })
            .HasDatabaseName("ix_atestados_paciente_criado")
            .IsDescending(false, true);

        builder.HasIndex(a => new { a.EstabelecimentoId, a.CriadoEm })
            .HasDatabaseName("ix_atestados_estab_criado")
            .IsDescending(false, true);

        builder.Ignore(a => a.DomainEvents);
    }
}

public class ModeloAtestadoConfiguration : IEntityTypeConfiguration<ModeloAtestado>
{
    public void Configure(EntityTypeBuilder<ModeloAtestado> builder)
    {
        builder.ToTable("modelos_atestado");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(m => m.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(m => m.ProfissionalUsuarioId).HasColumnName("profissional_usuario_id").IsRequired();
        builder.Property(m => m.Nome).HasColumnName("nome").HasMaxLength(120).IsRequired();
        builder.Property(m => m.Tipo).HasColumnName("tipo").HasMaxLength(20).HasConversion<string>().IsRequired();
        builder.Property(m => m.Conteudo).HasColumnName("conteudo").HasColumnType("text").IsRequired();
        builder.Property(m => m.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(m => m.AtualizadoEm).HasColumnName("atualizado_em");

        builder.HasIndex(m => new { m.EstabelecimentoId, m.Nome })
            .HasDatabaseName("ix_modelos_atestado_estab_nome");

        builder.Ignore(m => m.DomainEvents);
    }
}
