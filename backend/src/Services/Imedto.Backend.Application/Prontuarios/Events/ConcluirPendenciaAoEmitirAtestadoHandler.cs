using Imedto.Backend.Domain.Atestados.Events;
using Imedto.Backend.Domain.Prontuarios.Pendencias;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Prontuarios.Events;

/// <summary>
/// Ao emitir um atestado, conclui automaticamente a pendência mais recente
/// acao=CriarAtestado do mesmo paciente+tenant (R8/CA64).
/// Idempotente/no-op se não houver pendência aberta (R12/CA65).
/// </summary>
public class ConcluirPendenciaAoEmitirAtestadoHandler : IEventHandler<AtestadoEmitidoEvent>
{
    private readonly IPendenciaAtendimentoRepository _repo;

    public ConcluirPendenciaAoEmitirAtestadoHandler(IPendenciaAtendimentoRepository repo)
    {
        _repo = repo;
    }

    public async Task Handle(AtestadoEmitidoEvent domainEvent)
    {
        var pendencia = await _repo.ObterAbertaMaisRecentePorAcao(
            domainEvent.EstabelecimentoId,
            domainEvent.PacienteId,
            AcaoPendencia.CriarAtestado);

        if (pendencia is null)
            return; // no-op (R12/CA65)

        pendencia.ConcluirPorGatilho(domainEvent.AtestadoId);
    }
}
