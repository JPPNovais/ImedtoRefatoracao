using Imedto.Backend.Domain.Prontuarios.Pendencias;
using Imedto.Backend.Domain.Receitas.Events;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Prontuarios.Events;

/// <summary>
/// Ao emitir uma receita, conclui automaticamente a pendência mais recente
/// acao=CriarReceita do mesmo paciente+tenant (R7/CA63).
/// Idempotente/no-op se não houver pendência aberta (R12/CA65).
/// </summary>
public class ConcluirPendenciaAoEmitirReceitaHandler : IEventHandler<ReceitaEmitidaEvent>
{
    private readonly IPendenciaAtendimentoRepository _repo;

    public ConcluirPendenciaAoEmitirReceitaHandler(IPendenciaAtendimentoRepository repo)
    {
        _repo = repo;
    }

    public async Task Handle(ReceitaEmitidaEvent domainEvent)
    {
        var pendencia = await _repo.ObterAbertaMaisRecentePorAcao(
            domainEvent.EstabelecimentoId,
            domainEvent.PacienteId,
            AcaoPendencia.CriarReceita);

        if (pendencia is null)
            return; // no-op (R12/CA65)

        pendencia.ConcluirPorGatilho(domainEvent.ReceitaId);
    }
}
