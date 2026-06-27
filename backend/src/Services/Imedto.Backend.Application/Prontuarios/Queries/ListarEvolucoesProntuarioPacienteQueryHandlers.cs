using Imedto.Backend.Contracts.Prontuarios.Queries;
using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Prontuarios.Queries;

/// <summary>
/// Listagem paginada das evoluções do paciente. Audit LGPD: cada acesso registra
/// <see cref="TipoAcessoProntuario.Leitura"/> (histórico clínico).
/// </summary>
public class ListarEvolucoesProntuarioPacienteQueryHandlers
    : IRequestHandler<ListarEvolucoesProntuarioPacienteQuery, PaginaEvolucoesDto>
{
    private readonly ProntuarioQueryRepository _queryRepository;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IProntuarioRepository _prontuarioRepo;
    private readonly IProntuarioAcessoLogService _acessoLog;

    public ListarEvolucoesProntuarioPacienteQueryHandlers(
        ProntuarioQueryRepository queryRepository,
        IPacienteRepository pacienteRepo,
        IProntuarioRepository prontuarioRepo,
        IProntuarioAcessoLogService acessoLog)
    {
        _queryRepository = queryRepository;
        _pacienteRepo = pacienteRepo;
        _prontuarioRepo = prontuarioRepo;
        _acessoLog = acessoLog;
    }

    public async Task<PaginaEvolucoesDto> Handle(ListarEvolucoesProntuarioPacienteQuery query)
    {
        if (query.PacienteId <= 0)
            throw new BusinessException("Paciente é obrigatório.");

        var pagina = query.Pagina < 1 ? 1 : query.Pagina;
        var tamanho = query.TamanhoPagina is < 1 or > 100 ? 20 : query.TamanhoPagina;

        var paciente = await _pacienteRepo.ObterPorIdOuNulo(query.PacienteId, query.EstabelecimentoId)
            ?? throw new BusinessException("Paciente não encontrado.");

        var resultado = await _queryRepository.ListarEvolucoesPaginadas(
            paciente.Id, query.EstabelecimentoId, pagina, tamanho,
            query.SolicitanteUsuarioId, query.SolicitantePapel);

        // Audit só faz sentido quando há prontuário com evoluções acessadas.
        if (resultado.Total > 0 && query.SolicitanteUsuarioId != Guid.Empty)
        {
            var prontuario = await _prontuarioRepo.ObterPorPaciente(paciente.Id, query.EstabelecimentoId);
            if (prontuario is not null)
            {
                await _acessoLog.RegistrarAsync(
                    prontuario.Id,
                    query.SolicitanteUsuarioId,
                    query.EstabelecimentoId,
                    TipoAcessoProntuario.Leitura);
            }
        }

        return resultado;
    }
}
