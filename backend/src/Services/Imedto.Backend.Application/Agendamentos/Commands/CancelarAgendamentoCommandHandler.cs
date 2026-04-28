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
        var agendamento = await _agendamentoRepo.ObterPorId(cmd.AgendamentoId);

        if (agendamento.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Agendamento não encontrado neste estabelecimento.");

        agendamento.Cancelar(cmd.Motivo);
        await _agendamentoRepo.Salvar(agendamento);

        foreach (var ev in agendamento.DomainEvents)
            await _eventBus.Publish(ev);
        agendamento.ClearDomainEvents();
    }
}
