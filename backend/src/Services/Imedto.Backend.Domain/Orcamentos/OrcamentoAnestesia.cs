using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Orcamentos;

/// <summary>
/// Anestesia associada ao orçamento (item 6 — paridade com legado <c>orcamento_anestesia</c>).
/// Relação 1:1 com o orçamento — chave primária <see cref="OrcamentoId"/> impede mais de
/// uma anestesia por orçamento. Quando o orçamento prevê múltiplas anestesias, o caso
/// deve ser tratado como observação ou cirurgia separada — manter 1:1 reflete o fluxo
/// real (uma anestesia principal por procedimento).
/// </summary>
public class OrcamentoAnestesia : Entity
{
    public virtual long OrcamentoId { get; protected set; }
    public virtual TipoAnestesia TipoAnestesia { get; protected set; }
    public virtual decimal Valor { get; protected set; }
    public virtual string? Observacao { get; protected set; }

    protected OrcamentoAnestesia() { }

    internal static OrcamentoAnestesia Criar(
        long orcamentoId,
        TipoAnestesia tipo,
        decimal valor,
        string? observacao)
    {
        if (valor < 0)
            throw new BusinessException("Valor da anestesia não pode ser negativo.");
        if (observacao is { Length: > 200 })
            throw new BusinessException("Observação da anestesia não pode ter mais de 200 caracteres.");

        return new OrcamentoAnestesia
        {
            OrcamentoId = orcamentoId,
            TipoAnestesia = tipo,
            Valor = Math.Round(valor, 2),
            Observacao = observacao?.Trim()
        };
    }
}
