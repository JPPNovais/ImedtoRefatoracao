using Imedto.Backend.Contracts.Cobrancas.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Cobrancas.Queries;

/// <summary>Retorna o valor sugerido para o check-in (R2/CA2).</summary>
public class ObterValorSugeridoCheckInQuery : IQuery<ValorSugeridoCheckInDto>
{
    public long EstabelecimentoId { get; set; }
    public Guid ProfissionalUsuarioId { get; set; }
}
