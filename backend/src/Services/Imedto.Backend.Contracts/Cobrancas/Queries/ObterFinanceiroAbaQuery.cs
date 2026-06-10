using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.Contracts.Cobrancas.Queries.Results;

namespace Imedto.Backend.Contracts.Cobrancas.Queries;

/// <summary>
/// Query da aba Financeiro do paciente (F2).
/// Retorna KPIs + lista de cobranças com pagamentos/estornos do paciente no tenant.
/// Handler audita acesso via IPacienteAcessoLogService (R10/LGPD).
/// </summary>
public class ObterFinanceiroAbaQuery : IQuery<FinanceiroAbaDto>
{
    public long PacienteId { get; init; }
    public long EstabelecimentoId { get; init; }
    public Guid UsuarioId { get; init; }
}
