using Imedto.Backend.Contracts.Cirurgias.Queries;
using Imedto.Backend.Contracts.Cirurgias.Queries.Results;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Cirurgias.Queries;

/// <summary>
/// Scoped — depende de <see cref="IProntuarioAcessoLogService"/> para audit LGPD
/// (leitura de cirurgia = leitura de histórico clínico).
/// </summary>
public class ObterProcedimentoQueryHandlers
    : IRequestHandler<ObterProcedimentoQuery, ProcedimentoCirurgicoDto>,
      IRequestHandler<ListarProcedimentosDoPacienteQuery, IEnumerable<ProcedimentoCirurgicoResumoDto>>
{
    private readonly ProcedimentoCirurgicoQueryRepository _repo;
    private readonly IProntuarioAcessoLogService _acessoLog;

    public ObterProcedimentoQueryHandlers(
        ProcedimentoCirurgicoQueryRepository repo,
        IProntuarioAcessoLogService acessoLog)
    {
        _repo = repo;
        _acessoLog = acessoLog;
    }

    public async Task<ProcedimentoCirurgicoDto> Handle(ObterProcedimentoQuery query)
    {
        var dto = await _repo.ObterCompleto(query.ProcedimentoId, query.EstabelecimentoId)
            ?? throw new BusinessException("Procedimento não encontrado.");

        if (query.SolicitanteUsuarioId != Guid.Empty)
        {
            await _acessoLog.RegistrarAsync(
                dto.ProntuarioId, query.SolicitanteUsuarioId, query.EstabelecimentoId,
                TipoAcessoProntuario.Leitura);
        }
        return dto;
    }

    public Task<IEnumerable<ProcedimentoCirurgicoResumoDto>> Handle(ListarProcedimentosDoPacienteQuery query)
    {
        // Listagem não loga audit por item — auditoria de leitura por procedimento
        // ocorre no `Obter`. A tela apresenta uma lista mínima (sem PII clínica).
        return _repo.ListarDoPaciente(query.PacienteId, query.EstabelecimentoId);
    }
}
