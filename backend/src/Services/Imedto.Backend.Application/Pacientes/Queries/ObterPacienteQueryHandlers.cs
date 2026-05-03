using Imedto.Backend.Contracts.Pacientes.Queries;
using Imedto.Backend.Contracts.Pacientes.Queries.Results;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Pacientes.Queries;

/// <summary>
/// Handler de leitura de paciente. <b>Scoped</b> (nao Singleton) porque
/// audita acesso via <see cref="IPacienteAcessoLogService"/> (LGPD).
/// </summary>
public class ObterPacienteQueryHandlers : IRequestHandler<ObterPacienteQuery, PacienteDto>
{
    private readonly PacienteQueryRepository _queryRepository;
    private readonly IPacienteAcessoLogService _acessoLog;

    public ObterPacienteQueryHandlers(
        PacienteQueryRepository queryRepository,
        IPacienteAcessoLogService acessoLog)
    {
        _queryRepository = queryRepository;
        _acessoLog = acessoLog;
    }

    public async Task<PacienteDto> Handle(ObterPacienteQuery query)
    {
        var dto = await _queryRepository.ObterPorId(query.PacienteId, query.EstabelecimentoId);
        if (dto is null) return null;

        // Audit LGPD: registrar acesso de leitura aos dados pessoais. Falha de
        // gravacao do log nao quebra o fluxo (best-effort no service).
        await _acessoLog.RegistrarAsync(
            query.PacienteId, query.SolicitanteUsuarioId, query.EstabelecimentoId, TipoAcessoPaciente.Leitura);

        return dto;
    }
}
