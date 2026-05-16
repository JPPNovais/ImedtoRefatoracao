using Imedto.Backend.Domain.Inventario.Cadastros;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imedto.Backend.Infrastructure.Database.Configurations.Cadastros;

public class FornecedorEstoqueConfiguration : IEntityTypeConfiguration<FornecedorEstoque>
{
    public void Configure(EntityTypeBuilder<FornecedorEstoque> builder)
    {
        builder.ToTable("fornecedores_estoque");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(f => f.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(f => f.RazaoSocial).HasColumnName("razao_social").HasMaxLength(200).IsRequired();
        builder.Property(f => f.NomeFantasia).HasColumnName("nome_fantasia").HasMaxLength(150);
        builder.Property(f => f.Cnpj).HasColumnName("cnpj").HasMaxLength(14);
        builder.Property(f => f.ContatoNome).HasColumnName("contato_nome").HasMaxLength(150);
        builder.Property(f => f.ContatoTelefone).HasColumnName("contato_telefone").HasMaxLength(40);
        builder.Property(f => f.ContatoEmail).HasColumnName("contato_email").HasMaxLength(200);
        builder.Property(f => f.PrazoEntregaDias).HasColumnName("prazo_entrega_dias").IsRequired();
        builder.Property(f => f.Ativo).HasColumnName("ativo").IsRequired();
        builder.Property(f => f.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(f => f.AtualizadoEm).HasColumnName("atualizado_em");

        builder.HasIndex(f => new { f.EstabelecimentoId, f.Ativo })
            .HasDatabaseName("ix_fornecedores_estoque_estab_ativo");

        builder.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany()
            .HasForeignKey(f => f.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fornecedores_estoque_estabelecimento");

        builder.Ignore(f => f.DomainEvents);
    }
}
