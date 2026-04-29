using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Orcamentos.Commands;

/// <summary>
/// Cria um orçamento completo (cirúrgico ou simples-extendido). Aceita itens, equipe
/// (com comissão), implantes, formas de pagamento, cirurgias, internação, anestesia e
/// referência opcional ao procedimento cirúrgico. O comando simples
/// (<see cref="CriarOrcamentoCommand"/>) continua válido e cria orçamentos do tipo
/// <c>Simples</c>.
/// </summary>
public class CriarOrcamentoCompletoCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public long PacienteId { get; set; }
    public DateOnly Validade { get; set; }
    public string? Observacoes { get; set; }
    public Guid CriadoPorUsuarioId { get; set; }

    public string Tipo { get; set; } = "Simples";
    public long? ProcedimentoCirurgicoId { get; set; }
    /// <summary>
    /// Item 7 — schema fechado da configuração de pagamento. Substitui o antigo
    /// <c>ConfigPagamentoJson</c> opaco. <c>null</c> quando o orçamento não tem regra extra.
    /// </summary>
    public ConfigPagamentoOrcamentoDto? Configuracao { get; set; }
    public decimal DescontoBruto { get; set; }
    public decimal JurosBrutos { get; set; }

    public List<ItemOrcamentoPayload> Itens { get; set; } = new();
    public List<OrcamentoEquipePayload> Equipe { get; set; } = new();
    public List<OrcamentoImplantePayload> Implantes { get; set; } = new();
    public List<OrcamentoFormaPagamentoPayload> FormasPagamento { get; set; } = new();
    // Item 6 — paridade com legado.
    public List<OrcamentoCirurgiaPayload> Cirurgias { get; set; } = new();
    public OrcamentoInternacaoPayload? Internacao { get; set; }
    public OrcamentoAnestesiaPayload? Anestesia { get; set; }

    public long OrcamentoIdCriado { get; set; }
}

public record OrcamentoEquipePayload(Guid ProfissionalUsuarioId, string Papel, decimal Valor);
public record OrcamentoImplantePayload(long? ItemInventarioId, string Descricao, decimal Quantidade, decimal CustoUnitario);

/// <summary>
/// Forma de pagamento de um orçamento. Item 7 — incorpora <c>AcrescimoPercentual</c>
/// (juros aplicados na forma) e <c>EntradaPercentual</c> (% que vira entrada). Esses
/// campos antes ficavam apenas no JSON opaco e eram perdidos.
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
/// Internação 1:1 do orçamento. <c>Tipo</c> é string convertido pelo handler para o
/// enum <c>TipoInternacao</c> (Apartamento/Enfermaria/UTI/Ambulatorial).
/// </summary>
public record OrcamentoInternacaoPayload(string Tipo, int Dias, decimal ValorDiaria);

/// <summary>
/// Anestesia 1:1 do orçamento. <c>Tipo</c> é string convertido para o enum
/// <c>TipoAnestesia</c> (Local/Sedacao/Geral/Raquianestesia/Peridural/Bloqueio).
/// </summary>
public record OrcamentoAnestesiaPayload(string Tipo, decimal Valor, string? Observacao);
