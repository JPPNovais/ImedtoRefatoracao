using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Orcamentos.Commands;

/// <summary>
/// Cria um orçamento (aggregate único). Cirurgias, equipe, implantes, formas de
/// pagamento, local cirúrgico e anestesia são opcionais — o orçamento aceita qualquer
/// combinação desde que tenha pelo menos um item, implante, cirurgia ou comissão.
/// Nasce em <c>Rascunho</c> e só vai para <c>Enviado</c> via <see cref="EnviarOrcamentoCommand"/>.
/// </summary>
public class CriarOrcamentoCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public long PacienteId { get; set; }
    public DateOnly Validade { get; set; }
    public string? Observacoes { get; set; }
    public string? Titulo { get; set; }
    public Guid CriadoPorUsuarioId { get; set; }
    public long? ProcedimentoCirurgicoId { get; set; }
    public long? AgendamentoId { get; set; }

    public List<ItemOrcamentoPayload> Itens { get; set; } = new();
    public List<OrcamentoEquipePayload> Equipe { get; set; } = new();
    public List<OrcamentoImplantePayload> Implantes { get; set; } = new();
    public List<OrcamentoFormaPagamentoPayload> FormasPagamento { get; set; } = new();
    public List<OrcamentoCirurgiaPayload> Cirurgias { get; set; } = new();
    public OrcamentoLocalCirurgiaPayload? LocalCirurgia { get; set; }
    public OrcamentoAnestesiaPayload? Anestesia { get; set; }

    public long OrcamentoIdCriado { get; set; }
}

public record ItemOrcamentoPayload(
    string Descricao,
    decimal Quantidade,
    decimal ValorUnitario,
    decimal DescontoPercent);

public record OrcamentoEquipePayload(
    Guid ProfissionalUsuarioId,
    string Papel,
    decimal Valor);

public record OrcamentoImplantePayload(
    long? ItemInventarioId,
    string Descricao,
    decimal Quantidade,
    decimal CustoUnitario);

/// <summary>
/// Forma de pagamento de um orçamento. <c>AcrescimoPercentual</c> e
/// <c>EntradaPercentual</c> são por forma — descontos/acréscimos não vivem mais em jsonb
/// global, foram absorvidos aqui.
/// </summary>
public record OrcamentoFormaPagamentoPayload(
    long FormaPagamentoId,
    decimal Valor,
    int Parcelas,
    decimal AcrescimoPercentual,
    decimal EntradaPercentual,
    string? Observacao);

public record OrcamentoCirurgiaPayload(
    long? ProcedimentoCirurgicoId,
    string? Descricao,
    int Quantidade,
    int? DuracaoMinutos,
    decimal ValorTotal);

/// <summary>
/// Local cirúrgico do orçamento (paridade com legado — substitui o antigo
/// <c>OrcamentoInternacaoPayload</c>). <c>Tipo</c> é string convertido pelo handler
/// para <c>TipoLocalCirurgia</c> (5 valores: <c>IntLocal/IntPeridural/IntGeral/SemInternacao/Ambulatorio</c>).
/// <c>TempoMinutos</c> = tempo total da cirurgia. O valor é calculado server-side a partir
/// da <c>ConfiguracaoLocalCirurgia</c> do estabelecimento — não confiamos no que o cliente envia.
/// </summary>
public record OrcamentoLocalCirurgiaPayload(string Tipo, int TempoMinutos);

/// <summary>
/// Anestesia 1:1. <c>Tipo</c> é string convertido pelo handler para o enum
/// <c>TipoAnestesia</c>.
/// </summary>
public record OrcamentoAnestesiaPayload(string Tipo, decimal Valor, string? Observacao);
