using Imedto.Backend.Contracts.Pacientes.Queries;
using Imedto.Backend.Contracts.Pacientes.Queries.Results;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Pacientes.Queries;

/// <summary>
/// Relatório de acessos LGPD (Art. 9º/18).
/// Consolida paciente_acesso_log + prontuario_acesso_log em lista paginada leiga.
///
/// Gate: apenas Dono — aplicado no controller via [RequiresPapel(TenantPapel.Dono)].
/// Audit: a própria consulta ao relatório é auditada com TipoAcessoPaciente.Leitura
///        (R4/CA10) — 1 linha por carga, nunca por linha exibida.
///
/// Scoped — depende de IPacienteAcessoLogService (scoped).
/// </summary>
public class ListarAcessosDoPacienteQueryHandlers
    : IRequestHandler<ListarAcessosDoPacienteQuery, PaginaAcessosDto>
{
    private readonly IAcessoQueryRepository _queryRepo;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IPacienteAcessoLogService _acessoLog;

    public ListarAcessosDoPacienteQueryHandlers(
        IAcessoQueryRepository queryRepo,
        IPacienteRepository pacienteRepo,
        IPacienteAcessoLogService acessoLog)
    {
        _queryRepo = queryRepo;
        _pacienteRepo = pacienteRepo;
        _acessoLog = acessoLog;
    }

    public async Task<PaginaAcessosDto> Handle(ListarAcessosDoPacienteQuery query)
    {
        if (query.PacienteId <= 0)
            throw new BusinessException("Paciente é obrigatório.");

        var pagina  = query.Pagina < 1 ? 1 : query.Pagina;
        var tamanho = query.TamanhoPagina is < 1 or > 500 ? 20 : query.TamanhoPagina;

        // Defense-in-depth multi-tenant: valida posse do paciente no tenant (R2/CA8).
        // Mensagem genérica — não revela que o paciente pertence a outro tenant.
        var paciente = await _pacienteRepo.ObterPorIdOuNulo(query.PacienteId, query.EstabelecimentoId);
        if (paciente is null)
            throw new BusinessException("Paciente não encontrado.");

        var resultado = await _queryRepo.ListarDoPaciente(
            query.PacienteId,
            query.EstabelecimentoId,
            pagina,
            tamanho);

        // Audit LGPD: consultar o relatório de acessos é, por si, um acesso.
        // Registra 1 linha por carga de página (R4/CA10/CA11).
        await _acessoLog.RegistrarAsync(
            query.PacienteId,
            query.SolicitanteUsuarioId,
            query.EstabelecimentoId,
            TipoAcessoPaciente.Leitura);

        return resultado;
    }
}
