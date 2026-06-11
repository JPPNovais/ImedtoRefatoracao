using Imedto.Backend.Domain.Agendamentos.Events;
using Imedto.Backend.Domain.Prontuarios.Pendencias;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Prontuarios.Events;

/// <summary>
/// Ao criar um agendamento com InicioPrevisto no futuro, conclui automaticamente
/// a pendência mais recente acao=AgendarRetorno do mesmo paciente+tenant (R11/CA64).
///
/// Filtro de futuro (R11): evita que o próprio agendamento corrente (check-in mesmo dia)
/// conclua o retorno pendente de uma consulta futura.
///
/// Idempotente/no-op se não houver pendência aberta (R12/CA65).
/// </summary>
public class ConcluirPendenciaAoCriarAgendamentoHandler : IEventHandler<AgendamentoCriadoEvent>
{
    private readonly IPendenciaAtendimentoRepository _repo;

    public ConcluirPendenciaAoCriarAgendamentoHandler(IPendenciaAtendimentoRepository repo)
    {
        _repo = repo;
    }

    public async Task Handle(AgendamentoCriadoEvent domainEvent)
    {
        // Só conclui se o agendamento é para o futuro (R11)
        if (domainEvent.InicioPrevisto <= DateTime.UtcNow)
            return;

        var pendencia = await _repo.ObterAbertaMaisRecentePorAcao(
            domainEvent.EstabelecimentoId,
            domainEvent.PacienteId,
            AcaoPendencia.AgendarRetorno);

        if (pendencia is null)
            return; // no-op (R12/CA65)

        pendencia.ConcluirPorGatilho(domainEvent.AgendamentoId);
    }
}
