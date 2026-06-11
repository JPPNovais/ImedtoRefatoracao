using Imedto.Backend.Domain.Orcamentos.Events;
using Imedto.Backend.Domain.Prontuarios.Pendencias;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Prontuarios.Events;

/// <summary>
/// Ao criar um orçamento, conclui automaticamente a pendência mais recente
/// acao=CriarOrcamento do mesmo paciente+tenant (R10/CA64).
/// Idempotente/no-op se não houver pendência aberta (R12/CA65).
/// </summary>
public class ConcluirPendenciaAoCriarOrcamentoHandler : IEventHandler<OrcamentoCriadoEvent>
{
    private readonly IPendenciaAtendimentoRepository _repo;

    public ConcluirPendenciaAoCriarOrcamentoHandler(IPendenciaAtendimentoRepository repo)
    {
        _repo = repo;
    }

    public async Task Handle(OrcamentoCriadoEvent domainEvent)
    {
        var pendencia = await _repo.ObterAbertaMaisRecentePorAcao(
            domainEvent.EstabelecimentoId,
            domainEvent.PacienteId,
            AcaoPendencia.CriarOrcamento);

        if (pendencia is null)
            return; // no-op (R12/CA65)

        pendencia.ConcluirPorGatilho(domainEvent.OrcamentoId);
    }
}
