using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Orcamentos.Commands;

/// <summary>
/// Converte um orçamento <c>Aprovado</c> em um <c>ProcedimentoCirurgico</c> vinculado.
/// Idempotente por orçamento: só pode rodar uma vez (validação no domain).
/// </summary>
public class ConverterOrcamentoEmCirurgiaCommand : ICommand
{
    public long OrcamentoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public DateTime? DataAgendada { get; set; }
    public long ProcedimentoCirurgicoIdCriado { get; set; }
}
