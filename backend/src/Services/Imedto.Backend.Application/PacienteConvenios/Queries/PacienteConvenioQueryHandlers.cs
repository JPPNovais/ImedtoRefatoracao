using Imedto.Backend.Contracts.PacienteConvenios.Queries;
using Imedto.Backend.Contracts.PacienteConvenios.Queries.Results;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.PacienteConvenios.Queries;

/// <summary>
/// Lista carteirinhas do paciente para a aba Convênios.
/// Scoped: grava audit de Leitura (R15/CA151/D4) via IPacienteAcessoLogService.
/// </summary>
public class ListarPacienteConveniosQueryHandler :
    IRequestHandler<ListarPacienteConveniosQuery, IReadOnlyList<PacienteConvenioDto>>
{
    private readonly PacienteConvenioQueryRepository _repo;
    private readonly IPacienteAcessoLogService _acessoLog;

    public ListarPacienteConveniosQueryHandler(
        PacienteConvenioQueryRepository repo,
        IPacienteAcessoLogService acessoLog)
    {
        _repo = repo;
        _acessoLog = acessoLog;
    }

    public async Task<IReadOnlyList<PacienteConvenioDto>> Handle(ListarPacienteConveniosQuery query)
    {
        // CA151: audit de Leitura na aba Convênios (best-effort — não quebra a aba)
        _ = _acessoLog.RegistrarAsync(
            query.PacienteId,
            query.UsuarioSolicitanteId,
            query.EstabelecimentoId,
            TipoAcessoPaciente.Leitura);

        return await _repo.ListarPorPaciente(query.PacienteId, query.EstabelecimentoId);
    }
}

/// <summary>
/// Retorna carteirinhas ativas para pré-seleção no check-in (R8).
/// Singleton: sem audit (não é acesso à aba do paciente, é helper do fluxo de agendamento).
/// </summary>
public class ObterCarteirinhaAtivaCheckInQueryHandler :
    IRequestHandler<ObterCarteirinhaAtivaCheckInQuery, IReadOnlyList<CarteirinhaCheckInDto>>
{
    private readonly PacienteConvenioQueryRepository _repo;
    public ObterCarteirinhaAtivaCheckInQueryHandler(PacienteConvenioQueryRepository repo) => _repo = repo;

    public Task<IReadOnlyList<CarteirinhaCheckInDto>> Handle(ObterCarteirinhaAtivaCheckInQuery query)
        => _repo.ListarAtivasPorPacienteParaCheckIn(query.PacienteId, query.EstabelecimentoId);
}
