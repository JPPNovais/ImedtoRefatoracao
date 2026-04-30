using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Orcamentos.Catalogos;

/// <summary>
/// Catálogo de cirurgias para uso em orçamentos. Não confundir com
/// <c>ProcedimentoCirurgico</c> (instância concreta vinculada a um paciente) — este é
/// o template/preço-padrão que o usuário consulta ao montar um orçamento novo.
/// </summary>
public class CatalogoCirurgia : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual string Descricao { get; protected set; } = string.Empty;
    public virtual decimal ValorBase { get; protected set; }
    public virtual int? DuracaoPadraoMinutos { get; protected set; }
    public virtual bool Ativo { get; protected set; }
    public virtual DateTime CriadaEm { get; protected set; }
    public virtual DateTime? AtualizadaEm { get; protected set; }

    protected CatalogoCirurgia() { }

    public static CatalogoCirurgia Criar(
        long estabelecimentoId,
        string descricao,
        decimal valorBase,
        int? duracaoPadraoMinutos)
    {
        Validar(estabelecimentoId, descricao, valorBase, duracaoPadraoMinutos);
        return new CatalogoCirurgia
        {
            EstabelecimentoId = estabelecimentoId,
            Descricao = descricao.Trim(),
            ValorBase = valorBase,
            DuracaoPadraoMinutos = duracaoPadraoMinutos,
            Ativo = true,
            CriadaEm = DateTime.UtcNow
        };
    }

    public virtual void Atualizar(string descricao, decimal valorBase, int? duracaoPadraoMinutos)
    {
        Validar(EstabelecimentoId, descricao, valorBase, duracaoPadraoMinutos);
        Descricao = descricao.Trim();
        ValorBase = valorBase;
        DuracaoPadraoMinutos = duracaoPadraoMinutos;
        AtualizadaEm = DateTime.UtcNow;
    }

    public virtual void Inativar()
    {
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

    private static void Validar(long estabelecimentoId, string descricao, decimal valorBase, int? duracao)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (string.IsNullOrWhiteSpace(descricao))
            throw new BusinessException("Descrição da cirurgia é obrigatória.");
        if (valorBase < 0)
            throw new BusinessException("Valor base não pode ser negativo.");
        if (duracao is { } d && d <= 0)
            throw new BusinessException("Duração padrão deve ser positiva.");
    }
}
