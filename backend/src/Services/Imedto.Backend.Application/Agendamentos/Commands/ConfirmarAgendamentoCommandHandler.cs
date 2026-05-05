using Imedto.Backend.Contracts.Agendamentos.Commands;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Agendamentos.Commands;

public class ConfirmarAgendamentoCommandHandler : ICommandHandler<ConfirmarAgendamentoCommand>
{
    private readonly IAgendamentoRepository _agendamentoRepo;

    public ConfirmarAgendamentoCommandHandler(IAgendamentoRepository agendamentoRepo)
        => _agendamentoRepo = agendamentoRepo;

    public async Task Handle(ConfirmarAgendamentoCommand cmd)
    {
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var agendamento = await _agendamentoRepo.ObterPorIdOuNulo(cmd.AgendamentoId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Agendamento não encontrado.");

        agendamento.Confirmar();
        await _agendamentoRepo.Salvar(agendamento);
    }
}
