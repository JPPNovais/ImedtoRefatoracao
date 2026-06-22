using Imedto.Backend.Contracts.Prontuarios.Queries;
using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Prontuarios.Queries;

public class ObterProntuarioDoPacienteQueryHandlers
    : IRequestHandler<ObterProntuarioDoPacienteQuery, ProntuarioCompletoDto>
{
    private readonly ProntuarioQueryRepository _queryRepository;
    private readonly IProntuarioAcessoLogService _acessoLog;

    public ObterProntuarioDoPacienteQueryHandlers(
        ProntuarioQueryRepository queryRepository,
        IProntuarioAcessoLogService acessoLog)
    {
        _queryRepository = queryRepository;
        _acessoLog = acessoLog;
    }

    public async Task<ProntuarioCompletoDto> Handle(ObterProntuarioDoPacienteQuery query)
    {
        var dto = await _queryRepository.ObterDoPacienteGated(
            query.PacienteId,
            query.EstabelecimentoId,
            query.TamanhoTimeline,
            query.SolicitanteUsuarioId,
            query.SolicitantePapel);

        // Audit LGPD — registra a leitura (só quando há prontuário para logar).
        if (dto?.Prontuario is not null && query.SolicitanteUsuarioId != Guid.Empty)
        {
            await _acessoLog.RegistrarAsync(
                dto.Prontuario.Id,
                query.SolicitanteUsuarioId,
                query.EstabelecimentoId,
                TipoAcessoProntuario.Leitura);
        }

        return dto;
    }
}
