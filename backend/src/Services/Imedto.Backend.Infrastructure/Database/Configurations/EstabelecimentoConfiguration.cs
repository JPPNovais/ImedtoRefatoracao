using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Estabelecimentos;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class EstabelecimentoConfiguration : IEntityTypeConfiguration<Estabelecimento>
{
    public void Configure(EntityTypeBuilder<Estabelecimento> builder)
    {
        builder.ToTable("estabelecimentos");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(e => e.DonoUsuarioId).HasColumnName("dono_usuario_id").IsRequired();
        builder.Property(e => e.NomeFantasia).HasColumnName("nome_fantasia").IsRequired().HasMaxLength(200);
        builder.Property(e => e.RazaoSocial).HasColumnName("razao_social").HasMaxLength(200);
        builder.Property(e => e.Cnpj).HasColumnName("cnpj").HasMaxLength(14);
        builder.Property(e => e.Telefone).HasColumnName("telefone").HasMaxLength(20);
        builder.Property(e => e.Endereco).HasColumnName("endereco").HasMaxLength(500);
        builder.Property(e => e.FotoUrl).HasColumnName("foto_url").HasMaxLength(500);
        builder.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(e => e.AtualizadoEm).HasColumnName("atualizado_em");

        // Funcionamento
        builder.Property(e => e.HorarioInicio).HasColumnName("horario_inicio").HasColumnType("time").IsRequired()
            .HasDefaultValueSql("'08:00'::time");
        builder.Property(e => e.HorarioFim).HasColumnName("horario_fim").HasColumnType("time").IsRequired()
            .HasDefaultValueSql("'18:00'::time");
        builder.Property(e => e.DiasSemanaFuncionamentoJson).HasColumnName("dias_semana_funcionamento").HasColumnType("jsonb").IsRequired()
            .HasDefaultValueSql("'[1,2,3,4,5]'::jsonb");
        builder.Property(e => e.HorariosBloqueadosJson).HasColumnName("horarios_bloqueados").HasColumnType("jsonb").IsRequired()
            .HasDefaultValueSql("'[]'::jsonb");
        builder.Property(e => e.DatasBloqueadasJson).HasColumnName("datas_bloqueadas").HasColumnType("jsonb").IsRequired()
            .HasDefaultValueSql("'[]'::jsonb");

        // Read-only typed accessors — não são colunas, são desserializações dos JSONB acima.
        builder.Ignore(e => e.DiasSemanaFuncionamento);
        builder.Ignore(e => e.HorariosBloqueados);
        builder.Ignore(e => e.DatasBloqueadas);

        // Regra: um usuário só pode ser dono de um único estabelecimento.
        builder.HasIndex(e => e.DonoUsuarioId).IsUnique().HasDatabaseName("uq_estabelecimentos_dono");
        builder.HasIndex(e => e.Cnpj).IsUnique().HasDatabaseName("uq_estabelecimentos_cnpj")
            .HasFilter("cnpj IS NOT NULL");

        builder.Ignore(e => e.DomainEvents);
    }
}
