using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Orcamentos.Catalogos;

/// <summary>
/// Catálogo de equipes especializadas (ex: "Equipe de neurocirurgia", "Equipe de
/// videocirurgia"). Valor padrão é cobrado por orçamento que use a equipe — pode
/// ser ajustado individualmente em cada orçamento.
/// </summary>
public class CatalogoEquipeEspecializada : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual string Descricao { get; protected set; } = string.Empty;
    public virtual decimal ValorPadrao { get; protected set; }
    public virtual bool Ativo { get; protected set; }
    public virtual DateTime CriadaEm { get; protected set; }
    public virtual DateTime? AtualizadaEm { get; protected set; }

    protected CatalogoEquipeEspecializada() { }

    public static CatalogoEquipeEspecializada Criar(long estabelecimentoId, string descricao, decimal valorPadrao)
    {
        Validar(estabelecimentoId, descricao, valorPadrao);
        return new CatalogoEquipeEspecializada
        {
            EstabelecimentoId = estabelecimentoId,
            Descricao = descricao.Trim(),
            ValorPadrao = valorPadrao,
            Ativo = true,
            CriadaEm = DateTime.UtcNow
        };
    }

    public virtual void Atualizar(string descricao, decimal valorPadrao)
    {
        Validar(EstabelecimentoId, descricao, valorPadrao);
        Descricao = descricao.Trim();
        ValorPadrao = valorPadrao;
        AtualizadaEm = DateTime.UtcNow;
    }

    public virtual void Inativar() { if (Ativo) { Ativo = false; AtualizadaEm = DateTime.UtcNow; } }
    public virtual void Reativar() { if (!Ativo) { Ativo = true; AtualizadaEm = DateTime.UtcNow; } }

    private static void Validar(long estab, string descricao, decimal valor)
    {
        if (estab <= 0) throw new BusinessException("Estabelecimento é obrigatório.");
        if (string.IsNullOrWhiteSpace(descricao)) throw new BusinessException("Descrição é obrigatória.");
        if (valor < 0) throw new BusinessException("Valor não pode ser negativo.");
    }
}
