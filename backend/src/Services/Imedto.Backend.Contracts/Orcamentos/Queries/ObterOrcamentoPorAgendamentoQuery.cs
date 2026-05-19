using Imedto.Backend.Contracts.Orcamentos.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Orcamentos.Queries;

/// <summary>
/// Retorna o resumo do orçamento ativo (não cancelado/recusado) vinculado a um
/// agendamento, ou <c>null</c>. Usado pela UI da agenda para alternar entre
/// "Criar orçamento" e "Ver orçamento existente".
/// </summary>
public class ObterOrcamentoPorAgendamentoQuery : IQuery<OrcamentoResumoDto?>
{
    public long AgendamentoId { get; set; }
    public long EstabelecimentoId { get; set; }
}
