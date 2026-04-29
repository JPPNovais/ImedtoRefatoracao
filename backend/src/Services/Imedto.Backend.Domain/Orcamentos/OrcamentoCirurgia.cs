using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Orcamentos;

/// <summary>
/// Cirurgia listada dentro de um orçamento — equivale ao legado <c>orcamento_cirurgias</c>.
/// Permite que um único orçamento descreva um pacote com várias cirurgias (ex.: rinoplastia
/// + mentoplastia + lipo). Cada item soma <see cref="ValorTotal"/> ao bruto do orçamento.
///
/// Pode estar vinculada ao catálogo de procedimentos (<see cref="ProcedimentoCirurgicoId"/>)
/// ou ser texto livre (<see cref="Descricao"/>) — orçamento em fase de cotação aceita
/// cirurgias ainda não cadastradas.
/// </summary>
public class OrcamentoCirurgia : Entity
{
    public virtual long OrcamentoId { get; protected set; }
    public virtual long? ProcedimentoCirurgicoId { get; protected set; }
    public virtual string Descricao { get; protected set; } = string.Empty;
    public virtual int Quantidade { get; protected set; }
    public virtual int? DuracaoMinutos { get; protected set; }
    public virtual decimal ValorTotal { get; protected set; }
    public virtual int Ordem { get; protected set; }

    protected OrcamentoCirurgia() { }

    internal static OrcamentoCirurgia Criar(
        long orcamentoId,
        long? procedimentoCirurgicoId,
        string? descricao,
        int quantidade,
        int? duracaoMinutos,
        decimal valorTotal,
        int ordem)
    {
        var descricaoLimpa = descricao?.Trim();
        // Quando não há procedimento do catálogo, descrição é a única referência da cirurgia.
        if (procedimentoCirurgicoId is null && string.IsNullOrWhiteSpace(descricaoLimpa))
            throw new BusinessException("Descrição da cirurgia é obrigatória quando não há procedimento vinculado.");
        if (descricaoLimpa is { Length: > 200 })
            throw new BusinessException("Descrição da cirurgia não pode ter mais de 200 caracteres.");
        if (quantidade <= 0)
            throw new BusinessException("Quantidade da cirurgia deve ser maior que zero.");
        if (duracaoMinutos is { } d && d < 0)
            throw new BusinessException("Duração da cirurgia não pode ser negativa.");
        if (valorTotal < 0)
            throw new BusinessException("Valor total da cirurgia não pode ser negativo.");
        if (ordem < 0)
            throw new BusinessException("Ordem não pode ser negativa.");

        return new OrcamentoCirurgia
        {
            OrcamentoId = orcamentoId,
            ProcedimentoCirurgicoId = procedimentoCirurgicoId,
            Descricao = descricaoLimpa ?? string.Empty,
            Quantidade = quantidade,
            DuracaoMinutos = duracaoMinutos,
            ValorTotal = Math.Round(valorTotal, 2),
            Ordem = ordem
        };
    }
}
