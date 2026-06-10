using Imedto.Backend.Contracts.Cobrancas.Queries;
using Imedto.Backend.Contracts.Cobrancas.Queries.Results;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Infrastructure.Database.Repositories.Cobrancas;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Cobrancas.Queries;

/// <summary>
/// Handler da aba Financeiro do paciente (F2 — CA23/CA36).
/// Scoped: injeta IPacienteAcessoLogService (scoped, best-effort LGPD).
/// Falha do log não quebra o carregamento da aba (R10).
/// </summary>
public class ObterFinanceiroAbaQueryHandler : IRequestHandler<ObterFinanceiroAbaQuery, FinanceiroAbaDto>
{
    private readonly CobrancaQueryRepository _repo;
    private readonly IPacienteAcessoLogService _acessoLog;

    public ObterFinanceiroAbaQueryHandler(
        CobrancaQueryRepository repo,
        IPacienteAcessoLogService acessoLog)
    {
        _repo = repo;
        _acessoLog = acessoLog;
    }

    public async Task<FinanceiroAbaDto> Handle(ObterFinanceiroAbaQuery query)
    {
        // R10/CA36: audit de acesso (best-effort — falha não quebra o fluxo)
        _ = _acessoLog.RegistrarAsync(
            query.PacienteId,
            query.UsuarioId,
            query.EstabelecimentoId,
            TipoAcessoPaciente.Leitura);

        // R11: filtro por tenant implícito na query (paciente_id + estabelecimento_id)
        return await _repo.ObterFinanceiroAba(query.PacienteId, query.EstabelecimentoId);
    }
}
