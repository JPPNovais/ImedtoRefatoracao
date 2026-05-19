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
    public OrcamentoLocalCirurgiaPayload? LocalCirurgia { get; set; }
    public OrcamentoAnestesiaPayload? Anestesia { get; set; }

    /// <summary>Itens avulsos (orçamento não-cirúrgico).</summary>
    public List<ItemOrcamentoPayload> Itens { get; set; } = new();

    /// <summary>
    /// Equipe enriquecida com referência ao <c>valor_profissional_id</c> do catálogo
    /// para que o backend possa calcular honorário a partir do tempo. Opcional —
    /// quando não informado, o valor enviado em <see cref="Equipe"/> é usado tal e qual.
    /// </summary>
    public List<EquipeComCatalogoPayload> EquipeComCatalogo { get; set; } = new();
}

/// <summary>
/// Equipe enriquecida com referência ao catálogo <c>valor_profissional_id</c> +
/// tempo customizado em minutos. Quando informada, o handler busca a tabela de
/// honorários e devolve <c>valorCalculado</c> usando <c>CalcularValorProfissional</c>.
/// </summary>
public record EquipeComCatalogoPayload(
    long ValorProfissionalId,
    int Quantidade,
    int TempoMinutos);
