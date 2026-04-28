using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Orcamentos.Commands;

public class AtualizarOrcamentoCommand : ICommand
{
    public long OrcamentoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public DateOnly Validade { get; set; }
    public string? Observacoes { get; set; }
    public List<ItemOrcamentoPayload> Itens { get; set; } = new();
}
