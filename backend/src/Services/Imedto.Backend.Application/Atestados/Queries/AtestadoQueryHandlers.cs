using Imedto.Backend.Contracts.Atestados.Queries;
using Imedto.Backend.Contracts.Atestados.Queries.Results;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Atestados.Queries;

/// <summary>
/// Listagem de atestados do paciente. Audit LGPD: cada acesso é Leitura
/// (atestado é PII clínico ligado ao paciente).
/// Scoped — depende do <see cref="IProntuarioAcessoLogService"/> (Scoped).
/// </summary>
public class ListarAtestadosDoPacienteQueryHandlers
    : IRequestHandler<ListarAtestadosDoPacienteQuery, IReadOnlyList<AtestadoDto>>
{
    private readonly IAtestadoQueryRepository _queryRepo;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IProntuarioRepository _prontuarioRepo;
    private readonly IProntuarioAcessoLogService _acessoLog;

    public ListarAtestadosDoPacienteQueryHandlers(
        IAtestadoQueryRepository queryRepo,
        IPacienteRepository pacienteRepo,
        IProntuarioRepository prontuarioRepo,
        IProntuarioAcessoLogService acessoLog)
    {
        _queryRepo = queryRepo;
        _pacienteRepo = pacienteRepo;
        _prontuarioRepo = prontuarioRepo;
        _acessoLog = acessoLog;
    }

    public async Task<IReadOnlyList<AtestadoDto>> Handle(ListarAtestadosDoPacienteQuery query)
    {
        if (query.PacienteId <= 0)
            throw new BusinessException("Paciente é obrigatório.");

        var paciente = await _pacienteRepo.ObterPorIdOuNulo(query.PacienteId, query.EstabelecimentoId)
            ?? throw new BusinessException("Paciente não encontrado.");

        var atestados = await _queryRepo.ListarDoPaciente(query.PacienteId, query.EstabelecimentoId);

        var prontuario = await _prontuarioRepo.ObterPorPaciente(paciente.Id, query.EstabelecimentoId);
        if (prontuario is not null && atestados.Count > 0)
        {
            await _acessoLog.RegistrarAsync(
                prontuario.Id, query.SolicitanteUsuarioId, query.EstabelecimentoId, TipoAcessoProntuario.Leitura);
        }

        return atestados;
    }
}

public class ObterAtestadoQueryHandlers : IRequestHandler<ObterAtestadoQuery, AtestadoDto>
{
    private readonly IAtestadoQueryRepository _queryRepo;
    private readonly IProntuarioRepository _prontuarioRepo;
    private readonly IProntuarioAcessoLogService _acessoLog;

    public ObterAtestadoQueryHandlers(
        IAtestadoQueryRepository queryRepo,
        IProntuarioRepository prontuarioRepo,
        IProntuarioAcessoLogService acessoLog)
    {
        _queryRepo = queryRepo;
        _prontuarioRepo = prontuarioRepo;
        _acessoLog = acessoLog;
    }

    public async Task<AtestadoDto> Handle(ObterAtestadoQuery query)
    {
        var atestado = await _queryRepo.ObterPorId(query.AtestadoId, query.EstabelecimentoId)
            ?? throw new BusinessException("Atestado não encontrado.");

        var prontuario = await _prontuarioRepo.ObterPorPaciente(atestado.PacienteId, query.EstabelecimentoId);
        if (prontuario is not null)
        {
            await _acessoLog.RegistrarAsync(
                prontuario.Id, query.SolicitanteUsuarioId, query.EstabelecimentoId, TipoAcessoProntuario.Leitura);
        }

        return atestado;
    }
}

public class ListarModelosAtestadoQueryHandlers
    : IRequestHandler<ListarModelosAtestadoQuery, IReadOnlyList<ModeloAtestadoDto>>
{
    private readonly IAtestadoQueryRepository _queryRepo;

    public ListarModelosAtestadoQueryHandlers(IAtestadoQueryRepository queryRepo) => _queryRepo = queryRepo;

    public Task<IReadOnlyList<ModeloAtestadoDto>> Handle(ListarModelosAtestadoQuery query) =>
        _queryRepo.ListarModelos(query.EstabelecimentoId);
}
