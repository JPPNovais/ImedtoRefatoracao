using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Orcamentos;

/// <summary>
/// Internação associada ao orçamento (item 6 — paridade com legado <c>orcamento_internacao</c>).
/// Relação 1:1 com o orçamento — a chave primária <see cref="OrcamentoId"/> garante que
/// existe no máximo uma internação por orçamento. <see cref="ValorTotal"/> é snapshot
/// (dias × valor diária) calculado na criação para evitar drift com mudanças de tabela.
/// </summary>
public class OrcamentoInternacao : Entity
{
    public virtual long OrcamentoId { get; protected set; }
    public virtual TipoInternacao TipoInternacao { get; protected set; }
    public virtual int Dias { get; protected set; }
    public virtual decimal ValorDiaria { get; protected set; }
    public virtual decimal ValorTotal { get; protected set; }

    protected OrcamentoInternacao() { }

    internal static OrcamentoInternacao Criar(
        long orcamentoId,
        TipoInternacao tipo,
        int dias,
        decimal valorDiaria)
    {
        if (dias <= 0)
            throw new BusinessException("Dias de internação deve ser maior que zero.");
        if (valorDiaria < 0)
            throw new BusinessException("Valor da diária não pode ser negativo.");

        return new OrcamentoInternacao
        {
            OrcamentoId = orcamentoId,
            TipoInternacao = tipo,
            Dias = dias,
            ValorDiaria = Math.Round(valorDiaria, 2),
            ValorTotal = Math.Round(dias * valorDiaria, 2)
        };
    }
}
