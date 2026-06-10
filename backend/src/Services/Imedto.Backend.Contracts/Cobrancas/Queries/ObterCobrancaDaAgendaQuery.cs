using Imedto.Backend.Contracts.Cobrancas.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Cobrancas.Queries;

/// <summary>Retorna dados de cobrança/pagamento para o modal de pagamento.</summary>
public class ObterCobrancaDaAgendaQuery : IQuery<CobrancaDetalheDto?>
{
    public long AgendamentoId { get; set; }
    public long EstabelecimentoId { get; set; }
}
