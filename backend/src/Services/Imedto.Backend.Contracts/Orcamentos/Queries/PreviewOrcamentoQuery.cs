using Imedto.Backend.Contracts.Orcamentos.Commands;
using Imedto.Backend.Contracts.Orcamentos.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Orcamentos.Queries;

/// <summary>
/// Pré-visualização dos cálculos do orçamento sem persistir. Recebe o estado em
/// construção do form e devolve totais por linha, total geral e detalhamento das
/// formas de pagamento (com acréscimo, entrada e parcela). Cliente debouncia em
/// 250ms para evitar requisições por keystroke.
/// </summary>
public class PreviewOrcamentoQuery : IQuery<PreviewOrcamentoDto>
{
    public long EstabelecimentoId { get; set; }

    public List<OrcamentoEquipePayload> Equipe { get; set; } = new();
    public List<OrcamentoImplantePayload> Implantes { get; set; } = new();
    public List<OrcamentoFormaPagamentoPayload> FormasPagamento { get; set; } = new();
    public List<OrcamentoCirurgiaPayload> Cirurgias { get; set; } = new();
    public OrcamentoInternacaoPayload? Internacao { get; set; }
    public OrcamentoAnestesiaPayload? Anestesia { get; set; }

    /// <summary>Itens avulsos (orçamento não-cirúrgico).</summary>
    public List<ItemOrcamentoPayload> Itens { get; set; } = new();
}
