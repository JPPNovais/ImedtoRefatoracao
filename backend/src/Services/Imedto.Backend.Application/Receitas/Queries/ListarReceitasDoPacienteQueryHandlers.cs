using Imedto.Backend.Contracts.Receitas.Queries;
using Imedto.Backend.Contracts.Receitas.Queries.Results;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Receitas.Queries;

/// <summary>
/// Listagem paginada das receitas do paciente. Audit LGPD: cada acesso é registrado
/// como <see cref="TipoAcessoProntuario.Leitura"/> (medicação é PII clínico).
/// Scoped — depende de serviços scoped (acesso log + repos).
/// </summary>
public class ListarReceitasDoPacienteQueryHandlers
    : IRequestHandler<ListarReceitasDoPacienteQuery, PaginaReceitasDto>
{
    private readonly IReceitaQueryRepository _queryRepo;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IProntuarioRepository _prontuarioRepo;
    private readonly IProntuarioAcessoLogService _acessoLog;

    public ListarReceitasDoPacienteQueryHandlers(
        IReceitaQueryRepository queryRepo,
        IPacienteRepository pacienteRepo,
        IProntuarioRepository prontuarioRepo,
        IProntuarioAcessoLogService acessoLog)
    {
        _queryRepo = queryRepo;
        _pacienteRepo = pacienteRepo;
        _prontuarioRepo = prontuarioRepo;
        _acessoLog = acessoLog;
    }

    public async Task<PaginaReceitasDto> Handle(ListarReceitasDoPacienteQuery query)
    {
        if (query.PacienteId <= 0)
            throw new BusinessException("Paciente é obrigatório.");

        var pagina = query.Pagina < 1 ? 1 : query.Pagina;
        var tamanho = query.TamanhoPagina is < 1 or > 100 ? 20 : query.TamanhoPagina;

        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var paciente = await _pacienteRepo.ObterPorIdOuNulo(query.PacienteId, query.EstabelecimentoId)
            ?? throw new BusinessException("Paciente não encontrado.");

        var resultado = await _queryRepo.ListarDoPaciente(
            query.PacienteId, query.EstabelecimentoId, pagina, tamanho,
            query.SolicitanteUsuarioId, query.SolicitantePapel);

        // Registra acesso ao prontuário quando ele existe — listagem de receitas
        // é leitura clínica do histórico do paciente.
        var prontuario = await _prontuarioRepo.ObterPorPaciente(query.PacienteId, query.EstabelecimentoId);
        if (prontuario is not null)
        {
            await _acessoLog.RegistrarAsync(
                prontuario.Id,
                query.SolicitanteUsuarioId,
                query.EstabelecimentoId,
                TipoAcessoProntuario.Leitura);
        }

        return resultado;
    }
}
