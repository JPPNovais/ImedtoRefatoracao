using Imedto.Backend.Contracts.Agendamentos.Commands;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Agendamentos.Commands;

public class ConcluirAgendamentoCommandHandler : ICommandHandler<ConcluirAgendamentoCommand>
{
    private readonly IAgendamentoRepository _agendamentoRepo;

    public ConcluirAgendamentoCommandHandler(IAgendamentoRepository agendamentoRepo)
        => _agendamentoRepo = agendamentoRepo;

    public async Task Handle(ConcluirAgendamentoCommand cmd)
    {
        var agendamento = await _agendamentoRepo.ObterPorId(cmd.AgendamentoId);

        if (agendamento.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Agendamento não encontrado neste estabelecimento.");

        agendamento.Concluir();
        await _agendamentoRepo.Salvar(agendamento);
    }
}
