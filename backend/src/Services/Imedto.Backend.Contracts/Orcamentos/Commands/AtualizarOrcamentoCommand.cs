using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Orcamentos.Commands;

/// <summary>
/// Substitui o aggregate inteiro (itens + equipe + implantes + formas + cirurgias +
/// local cirúrgico + anestesia). Permitido apenas em <c>Rascunho</c> ou <c>Enviado</c>.
/// </summary>
public class AtualizarOrcamentoCommand : ICommand
{
    public long OrcamentoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public DateOnly Validade { get; set; }
    public string? Observacoes { get; set; }
    public string? Titulo { get; set; }
    public long? ProcedimentoCirurgicoId { get; set; }
    public long? AgendamentoId { get; set; }

    public List<ItemOrcamentoPayload> Itens { get; set; } = new();
    public List<OrcamentoEquipePayload> Equipe { get; set; } = new();
    public List<OrcamentoImplantePayload> Implantes { get; set; } = new();
    public List<OrcamentoFormaPagamentoPayload> FormasPagamento { get; set; } = new();
    public List<OrcamentoCirurgiaPayload> Cirurgias { get; set; } = new();
    public OrcamentoLocalCirurgiaPayload? LocalCirurgia { get; set; }
    public OrcamentoAnestesiaPayload? Anestesia { get; set; }
}
