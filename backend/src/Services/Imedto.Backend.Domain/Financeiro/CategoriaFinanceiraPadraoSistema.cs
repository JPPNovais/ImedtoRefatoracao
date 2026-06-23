using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Financeiro;

/// <summary>
/// Catálogo global de categorias financeiras padrão mantido pelo admin da plataforma.
/// Registros com <see cref="Ativo"/> = true são propagados (por cópia) a todos os
/// estabelecimentos no momento da criação do estabelecimento e ao criar/inativar/reativar
/// uma categoria global.
///
/// Sem <c>EstabelecimentoId</c> — escopo plataforma. Acesso exclusivo pelo admin global.
/// O <see cref="Nome"/> é chave de negócio e é imutável após a criação (R5 do briefing
/// 2026-06-22_003): para renomear, inativa-se a antiga e cria-se uma nova.
/// </summary>
public class CategoriaFinanceiraPadraoSistema : Entity
{
    public virtual string Nome { get; protected set; } = string.Empty;
    public virtual TipoCategoria Tipo { get; protected set; }
    public virtual bool Ativo { get; protected set; }
    public virtual DateTime CriadaEm { get; protected set; }
    public virtual DateTime? AtualizadaEm { get; protected set; }

    protected CategoriaFinanceiraPadraoSistema() { }

    public static CategoriaFinanceiraPadraoSistema Criar(string nome, TipoCategoria tipo)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome da categoria é obrigatório.");
        if (nome.Trim().Length > 80)
            throw new BusinessException("Nome da categoria não pode exceder 80 caracteres.");

        return new CategoriaFinanceiraPadraoSistema
        {
            Nome = nome.Trim(),
            Tipo = tipo,
            Ativo = true,
            CriadaEm = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Inativa o registro global. Handlers de propagação devem refletir a inativação
    /// nas cópias dos estabelecimentos (mesmo Nome+Tipo, Padrao=true).
    /// </summary>
    public virtual void Inativar()
    {
        if (!Ativo) return;
        Ativo = false;
        AtualizadaEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Reativa o registro global. Handlers de propagação devem refletir a reativação
    /// nas cópias dos estabelecimentos (mesmo Nome+Tipo, Padrao=true), incluindo as
    /// que o próprio estabelecimento tenha inativado manualmente (R4.1).
    /// </summary>
    public virtual void Reativar()
    {
        if (Ativo) return;
        Ativo = true;
        AtualizadaEm = DateTime.UtcNow;
    }
}
