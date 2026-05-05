using Imedto.Backend.Contracts.Agendamentos.Commands;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Agendamentos.Commands;

public class CancelarAgendamentoCommandHandler : ICommandHandler<CancelarAgendamentoCommand>
{
    private readonly IAgendamentoRepository _agendamentoRepo;
    private readonly IEventBus _eventBus;

    public CancelarAgendamentoCommandHandler(
        IAgendamentoRepository agendamentoRepo,
        IEventBus eventBus)
    {
        _agendamentoRepo = agendamentoRepo;
        _eventBus = eventBus;
    }

    public async Task Handle(CancelarAgendamentoCommand cmd)
    {
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var agendamento = await _agendamentoRepo.ObterPorIdOuNulo(cmd.AgendamentoId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Agendamento não encontrado.");

        agendamento.Cancelar(cmd.Motivo);
        await _agendamentoRepo.Salvar(agendamento);

        foreach (var ev in agendamento.DomainEvents)
            await _eventBus.Publish(ev);
        agendamento.ClearDomainEvents();
    }
}
