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
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var agendamento = await _agendamentoRepo.ObterPorIdOuNulo(cmd.AgendamentoId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Agendamento não encontrado.");

        agendamento.Concluir();
        await _agendamentoRepo.Salvar(agendamento);
    }
}
