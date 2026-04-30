using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Orcamentos.Commands;

/// <summary>Cancela orçamento em qualquer status não-terminal.</summary>
public class CancelarOrcamentoCommand : ICommand
{
    public long OrcamentoId { get; set; }
    public long EstabelecimentoId { get; set; }
}
