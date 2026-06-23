using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Financeiro;

/// <summary>
/// Categoria financeira (receita/despesa) associada a um estabelecimento. Categorias com
/// <see cref="Padrao"/> = true são criadas pelo seed automático ao criar o estabelecimento
/// e não podem ser editadas/inativadas pelos handlers comuns — fazem parte do contrato
/// mínimo do produto.
/// </summary>
public class CategoriaFinanceira : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual string Nome { get; protected set; } = string.Empty;
    public virtual TipoCategoria Tipo { get; protected set; }
    public virtual bool Padrao { get; protected set; }
    public virtual bool Ativo { get; protected set; }
    public virtual DateTime CriadaEm { get; protected set; }
    public virtual DateTime? AtualizadaEm { get; protected set; }

    protected CategoriaFinanceira() { }

    /// <summary>Cria categoria customizada (não-padrão), criada manualmente pelo usuário.</summary>
    public static CategoriaFinanceira Criar(long estabelecimentoId, string nome, TipoCategoria tipo)
        => Construir(estabelecimentoId, nome, tipo, padrao: false);

    /// <summary>Cria categoria padrão (seed). Não pode ser editada/inativada após criada.</summary>
    public static CategoriaFinanceira CriarPadrao(long estabelecimentoId, string nome, TipoCategoria tipo)
        => Construir(estabelecimentoId, nome, tipo, padrao: true);

    private static CategoriaFinanceira Construir(long estabelecimentoId, string nome, TipoCategoria tipo, bool padrao)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome da categoria é obrigatório.");

        return new CategoriaFinanceira
        {
            EstabelecimentoId = estabelecimentoId,
            Nome = nome.Trim(),
            Tipo = tipo,
            Padrao = padrao,
            Ativo = true,
            CriadaEm = DateTime.UtcNow
        };
    }

    public virtual void Atualizar(string nome, TipoCategoria tipo)
    {
        if (Padrao)
            throw new BusinessException("Categoria padrão não pode ser editada.");
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome da categoria é obrigatório.");

        Nome = nome.Trim();
        Tipo = tipo;
        AtualizadaEm = DateTime.UtcNow;
    }

    public virtual void Inativar()
    {
        // R8 (briefing 2026-06-22_003): Padrao=true pode ser inativado pelo estabelecimento.
        // Somente Atualizar() e exclusão permanecem bloqueados para Padrao=true.
        if (!Ativo) return;

        Ativo = false;
        AtualizadaEm = DateTime.UtcNow;
    }

    public virtual void Reativar()
    {
        if (Ativo) return;
        Ativo = true;
        AtualizadaEm = DateTime.UtcNow;
    }
}
