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
    private readonly IProntuarioAcessoLogService _acessoLog;

    public ObterExameFisicoQueryHandlers(
        ExameFisicoQueryRepository queryRepo,
        IProntuarioAcessoLogService acessoLog)
    {
        _queryRepo = queryRepo;
        _acessoLog = acessoLog;
    }

    public async Task<ExameFisicoDto?> Handle(ObterExameFisicoQuery query)
    {
        var dto = await _queryRepo.ObterCompleto(query.ExameFisicoId, query.EstabelecimentoId);
        if (dto is null) return null;

        await _acessoLog.RegistrarAsync(
            dto.ProntuarioId, query.SolicitanteUsuarioId, query.EstabelecimentoId, TipoAcessoProntuario.Leitura);

        return dto;
    }

    public async Task<ExameFisicoDto?> Handle(ObterExameFisicoPorEvolucaoQuery query)
    {
        var dto = await _queryRepo.ObterPorEvolucao(query.EvolucaoId, query.EstabelecimentoId);
        if (dto is null) return null;

        await _acessoLog.RegistrarAsync(
            dto.ProntuarioId, query.SolicitanteUsuarioId, query.EstabelecimentoId, TipoAcessoProntuario.Leitura);

        return dto;
    }

    public async Task<PaginaExamesFisicosDto> Handle(ListarExamesFisicosDoPacienteQuery query)
    {
        var pagina = Math.Max(1, query.Pagina);
        var tamanho = Math.Clamp(query.Tamanho, 1, 100);

        var resultado = await _queryRepo.ListarDoPaciente(
            query.PacienteId, query.EstabelecimentoId, pagina, tamanho);

        // Audit LGPD: consultar a lista de exames do paciente é leitura sensível.
        // Auditamos mesmo se vazio — saber que o usuário "olhou" já é informação relevante.
        if (resultado.Total > 0)
        {
            await _acessoLog.RegistrarAsync(
                resultado.PrimeiroProntuarioId, query.SolicitanteUsuarioId, query.EstabelecimentoId, TipoAcessoProntuario.Leitura);
        }

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
        var resultado = await _queryRepo.Timeline(query.PacienteId, query.EstabelecimentoId, ate);

        if (resultado.PrimeiroProntuarioId > 0)
        {
            await _acessoLog.RegistrarAsync(
                resultado.PrimeiroProntuarioId, query.SolicitanteUsuarioId, query.EstabelecimentoId, TipoAcessoProntuario.Leitura);
        }

        return resultado.Itens;
    }
}
