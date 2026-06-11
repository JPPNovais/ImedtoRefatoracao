using Imedto.Backend.Domain.PedidosExame.Events;
using Imedto.Backend.Domain.Prontuarios.Pendencias;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Prontuarios.Events;

/// <summary>
/// Ao emitir um pedido de exame, conclui automaticamente a pendência mais recente
/// acao=PedirExame do mesmo paciente+tenant (R9/CA64).
/// Idempotente/no-op se não houver pendência aberta (R12/CA65).
/// </summary>
public class ConcluirPendenciaAoEmitirPedidoExameHandler : IEventHandler<PedidoExameEmitidoEvent>
{
    private readonly IPendenciaAtendimentoRepository _repo;

    public ConcluirPendenciaAoEmitirPedidoExameHandler(IPendenciaAtendimentoRepository repo)
    {
        _repo = repo;
    }

    public async Task Handle(PedidoExameEmitidoEvent domainEvent)
    {
        var pendencia = await _repo.ObterAbertaMaisRecentePorAcao(
            domainEvent.EstabelecimentoId,
            domainEvent.PacienteId,
            AcaoPendencia.PedirExame);

        if (pendencia is null)
            return; // no-op (R12/CA65)

        pendencia.ConcluirPorGatilho(domainEvent.PedidoExameId);
    }
}
