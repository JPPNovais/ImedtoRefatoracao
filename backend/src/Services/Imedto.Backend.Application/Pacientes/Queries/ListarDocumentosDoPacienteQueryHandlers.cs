using Imedto.Backend.Contracts.Pacientes.Queries;
using Imedto.Backend.Contracts.Pacientes.Queries.Results;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Pacientes.Queries;

/// <summary>
/// Query agregada de documentos clínicos finalizados de um paciente.
/// Consolida receitas (Emitida), atestados e pedidos de exame numa lista unificada
/// paginada server-side — somente leitura, sem CRUD.
///
/// LGPD: registra 1 acesso de leitura ao prontuário por carga (R4/CA12),
/// seguindo o precedente de ListarReceitasDoPacienteQueryHandlers.
/// Scoped — depende de IProntuarioAcessoLogService (scoped).
/// </summary>
public class ListarDocumentosDoPacienteQueryHandlers
    : IRequestHandler<ListarDocumentosDoPacienteQuery, PaginaDocumentosDto>
{
    private readonly IDocumentoQueryRepository _queryRepo;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IProntuarioRepository _prontuarioRepo;
    private readonly IProntuarioAcessoLogService _acessoLog;

    public ListarDocumentosDoPacienteQueryHandlers(
        IDocumentoQueryRepository queryRepo,
        IPacienteRepository pacienteRepo,
        IProntuarioRepository prontuarioRepo,
        IProntuarioAcessoLogService acessoLog)
    {
        _queryRepo = queryRepo;
        _pacienteRepo = pacienteRepo;
        _prontuarioRepo = prontuarioRepo;
        _acessoLog = acessoLog;
    }

    public async Task<PaginaDocumentosDto> Handle(ListarDocumentosDoPacienteQuery query)
    {
        if (query.PacienteId <= 0)
            throw new BusinessException("Paciente é obrigatório.");

        var pagina   = query.Pagina < 1 ? 1 : query.Pagina;
        var tamanho  = query.TamanhoPagina is < 1 or > 100 ? 20 : query.TamanhoPagina;

        // Defense-in-depth multi-tenant: validação de posse do paciente no tenant (R2/CA10).
        var paciente = await _pacienteRepo.ObterPorIdOuNulo(query.PacienteId, query.EstabelecimentoId);
        if (paciente is null)
            throw new BusinessException("Paciente não encontrado.");

        // Normaliza busca vazia → nulo para que a query não aplique filtro desnecessário (R11).
        var buscaNorm = string.IsNullOrWhiteSpace(query.Busca) ? null : query.Busca.Trim();

        var resultado = await _queryRepo.ListarDoPaciente(
            query.PacienteId,
            query.EstabelecimentoId,
            pagina,
            tamanho,
            query.Tipo,
            query.DataInicio,
            query.DataFim,
            buscaNorm,
            query.SolicitanteUsuarioId,
            query.SolicitantePapel);

        // Audit LGPD: listagem de documentos clínicos é leitura de prontuário.
        // Registra 1 acesso por carga (não 1 por resultado — CA12/CA30).
        // LGPD: busca NÃO vai ao log (R13.a) — apenas tipo e ação de leitura.
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
