using Imedto.Backend.Contracts.Prontuarios.Queries;
using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Prontuarios.Queries;

/// <summary>
/// Handlers das queries de Exame Físico. <b>Scoped</b> (não singleton) porque
/// auditam acesso de leitura via <see cref="IProntuarioAcessoLogService"/> (LGPD).
/// </summary>
public class ObterExameFisicoQueryHandlers
    : IRequestHandler<ObterExameFisicoQuery, ExameFisicoDto?>,
      IRequestHandler<ObterExameFisicoPorEvolucaoQuery, ExameFisicoDto?>,
      IRequestHandler<ListarExamesFisicosDoPacienteQuery, PaginaExamesFisicosDto>,
      IRequestHandler<TimelineExamesFisicosQuery, IEnumerable<ExameFisicoResumoDto>>
{
    private readonly ExameFisicoQueryRepository _queryRepo;
    private readonly IProntuarioRepository _prontuarioRepo;
    private readonly IProntuarioAcessoLogService _acessoLog;

    public ObterExameFisicoQueryHandlers(
        ExameFisicoQueryRepository queryRepo,
        IProntuarioRepository prontuarioRepo,
        IProntuarioAcessoLogService acessoLog)
    {
        _queryRepo = queryRepo;
        _prontuarioRepo = prontuarioRepo;
        _acessoLog = acessoLog;
    }

    public async Task<ExameFisicoDto?> Handle(ObterExameFisicoQuery query)
    {
        var resultado = await _queryRepo.ObterCompleto(query.ExameFisicoId, query.EstabelecimentoId);
        if (resultado is null) return null;

        var (exame, prontuarioId) = resultado.Value;
        await _acessoLog.RegistrarAsync(
            prontuarioId, query.SolicitanteUsuarioId, query.EstabelecimentoId, TipoAcessoProntuario.Leitura);

        return exame;
    }

    public async Task<ExameFisicoDto?> Handle(ObterExameFisicoPorEvolucaoQuery query)
    {
        var resultado = await _queryRepo.ObterPorEvolucao(query.EvolucaoId, query.EstabelecimentoId);
        if (resultado is null) return null;

        var (exame, prontuarioId) = resultado.Value;
        await _acessoLog.RegistrarAsync(
            prontuarioId, query.SolicitanteUsuarioId, query.EstabelecimentoId, TipoAcessoProntuario.Leitura);

        return exame;
    }

    public async Task<PaginaExamesFisicosDto> Handle(ListarExamesFisicosDoPacienteQuery query)
    {
        var pagina = Math.Max(1, query.Pagina);
        var tamanho = Math.Clamp(query.Tamanho, 1, 100);

        // Audit LGPD: consulta a lista de exames eh leitura sensivel. Auditar mesmo
        // se a lista vier vazia — "olhou e nao tinha" tambem eh informacao relevante.
        // ObterPorPaciente ja filtra por estabelecimentoId (defense-in-depth multi-tenant).
        var prontuario = await _prontuarioRepo.ObterPorPaciente(query.PacienteId, query.EstabelecimentoId);
        if (prontuario is not null)
        {
            await _acessoLog.RegistrarAsync(
                prontuario.Id, query.SolicitanteUsuarioId, query.EstabelecimentoId, TipoAcessoProntuario.Leitura);
        }

        var resultado = await _queryRepo.ListarDoPaciente(
            query.PacienteId, query.EstabelecimentoId, pagina, tamanho);

        return new PaginaExamesFisicosDto
        {
            Itens = resultado.Itens,
            Total = resultado.Total,
            Pagina = pagina,
            Tamanho = tamanho
        };
    }

    public async Task<IEnumerable<ExameFisicoResumoDto>> Handle(TimelineExamesFisicosQuery query)
    {
        var ate = Math.Clamp(query.Ate, 1, 50);

        // Audit LGPD: idem Listar — auditar incondicionalmente quando ha prontuario.
        var prontuario = await _prontuarioRepo.ObterPorPaciente(query.PacienteId, query.EstabelecimentoId);
        if (prontuario is not null)
        {
            await _acessoLog.RegistrarAsync(
                prontuario.Id, query.SolicitanteUsuarioId, query.EstabelecimentoId, TipoAcessoProntuario.Leitura);
        }

        var resultado = await _queryRepo.Timeline(query.PacienteId, query.EstabelecimentoId, ate);
        return resultado.Itens;
    }
}
