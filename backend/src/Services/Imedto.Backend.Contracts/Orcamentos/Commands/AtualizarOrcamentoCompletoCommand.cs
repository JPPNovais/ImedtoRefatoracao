using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Orcamentos.Commands;

public class AtualizarOrcamentoCompletoCommand : ICommand
{
    public long OrcamentoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public DateOnly Validade { get; set; }
    public string? Observacoes { get; set; }

    public string Tipo { get; set; } = "Simples";
    public long? ProcedimentoCirurgicoId { get; set; }
    /// <summary>Item 7 — schema fechado, substitui <c>ConfigPagamentoJson</c>.</summary>
    public ConfigPagamentoOrcamentoDto? Configuracao { get; set; }
    public decimal DescontoBruto { get; set; }
    public decimal JurosBrutos { get; set; }

    public List<ItemOrcamentoPayload> Itens { get; set; } = new();
    public List<OrcamentoEquipePayload> Equipe { get; set; } = new();
    public List<OrcamentoImplantePayload> Implantes { get; set; } = new();
    public List<OrcamentoFormaPagamentoPayload> FormasPagamento { get; set; } = new();
    // Item 6.
    public List<OrcamentoCirurgiaPayload> Cirurgias { get; set; } = new();
    public OrcamentoInternacaoPayload? Internacao { get; set; }
    public OrcamentoAnestesiaPayload? Anestesia { get; set; }
}
