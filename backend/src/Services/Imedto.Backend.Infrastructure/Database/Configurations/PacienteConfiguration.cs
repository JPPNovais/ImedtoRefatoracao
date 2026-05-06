using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Pacientes;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class PacienteConfiguration : IEntityTypeConfiguration<Paciente>
{
    public void Configure(EntityTypeBuilder<Paciente> builder)
    {
        builder.ToTable("pacientes");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(p => p.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(p => p.NomeCompleto).HasColumnName("nome_completo").IsRequired().HasMaxLength(200);
        builder.Property(p => p.Cpf).HasColumnName("cpf").HasMaxLength(11);
        builder.Property(p => p.DocumentoInternacional)
            .HasColumnName("documento_internacional")
            .HasMaxLength(Paciente.DocumentoInternacionalMaxLen);
        builder.Property(p => p.DataNascimento).HasColumnName("data_nascimento").HasColumnType("date");
        builder.Property(p => p.Genero).HasColumnName("genero").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.Telefone).HasColumnName("telefone").HasMaxLength(20);
        builder.Property(p => p.Email).HasColumnName("email").HasMaxLength(320);
        builder.Property(p => p.Endereco).HasColumnName("endereco").HasMaxLength(500);
        builder.Property(p => p.Observacoes).HasColumnName("observacoes").HasMaxLength(2000);

        // text[] no Postgres — Npgsql mapeia diretamente para IReadOnlyList<string>.
        // O domain garante normalização (trim, dedupe case-insensitive) e limites.
        builder.Property(p => p.Tags)
            .HasColumnName("tags")
            .HasColumnType("text[]")
            .IsRequired()
            .HasDefaultValueSql("ARRAY[]::text[]");
        builder.Property(p => p.Alertas)
            .HasColumnName("alertas")
            .HasColumnType("text[]")
            .IsRequired()
            .HasDefaultValueSql("ARRAY[]::text[]");
        builder.Property(p => p.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(p => p.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(p => p.DeletadoEm).HasColumnName("deletado_em");
        builder.Property(p => p.DeletadoPorUsuarioId).HasColumnName("deletado_por_usuario_id");

        // Item 4.3 — anonimização LGPD.
        builder.Property(p => p.AnonimizadoEm).HasColumnName("anonimizado_em");
        builder.Property(p => p.AnonimizadoPorUsuarioId).HasColumnName("anonimizado_por_usuario_id");

        // Listagens ativas sempre filtram por estabelecimento + não-deletado.
        builder.HasIndex(p => new { p.EstabelecimentoId, p.DeletadoEm }).HasDatabaseName("ix_pacientes_estabelecimento");

        // Unique por (estabelecimento, CPF) — CPF pode repetir em estabelecimentos diferentes.
        builder.HasIndex(p => new { p.EstabelecimentoId, p.Cpf })
            .IsUnique()
            .HasDatabaseName("uq_pacientes_estabelecimento_cpf")
            .HasFilter("cpf IS NOT NULL AND deletado_em IS NULL");

        // Unique por (estabelecimento, documento_internacional) — mesmo critério do CPF.
        builder.HasIndex(p => new { p.EstabelecimentoId, p.DocumentoInternacional })
            .IsUnique()
            .HasDatabaseName("uq_pacientes_estabelecimento_doc_internacional")
            .HasFilter("documento_internacional IS NOT NULL AND deletado_em IS NULL");

        builder.Ignore(p => p.DomainEvents);
        builder.Ignore(p => p.EstaDeletado);
        builder.Ignore(p => p.EstaAnonimizado);
    }
}
