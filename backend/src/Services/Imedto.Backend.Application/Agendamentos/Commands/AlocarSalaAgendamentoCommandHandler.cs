using Imedto.Backend.Contracts.Agendamentos.Commands;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Salas;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Agendamentos.Commands;

public class AlocarSalaAgendamentoCommandHandler : ICommandHandler<AlocarSalaAgendamentoCommand>
{
    private readonly IAgendamentoRepository _agendamentoRepo;
    private readonly ISalaRepository _salaRepo;
    private readonly IAgendamentoSalaAuditRepository _auditRepo;

    public AlocarSalaAgendamentoCommandHandler(
        IAgendamentoRepository agendamentoRepo,
        ISalaRepository salaRepo,
        IAgendamentoSalaAuditRepository auditRepo)
    {
        _agendamentoRepo = agendamentoRepo;
        _salaRepo = salaRepo;
        _auditRepo = auditRepo;
    }

    public async Task Handle(AlocarSalaAgendamentoCommand cmd)
    {
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var agendamento = await _agendamentoRepo.ObterPorIdOuNulo(cmd.AgendamentoId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Agendamento não encontrado.");

        if (cmd.SalaId.HasValue)
        {
            var sala = await _salaRepo.ObterPorIdOuNulo(cmd.SalaId.Value, cmd.EstabelecimentoId)
                ?? throw new BusinessException("Sala não encontrada ou inativa.");
            if (!sala.Ativo)
                throw new BusinessException("Sala não encontrada ou inativa.");
        }

        var salaIdAnterior = agendamento.SalaId;
        agendamento.AlocarSala(cmd.SalaId);
        await _agendamentoRepo.Salvar(agendamento);

        if (salaIdAnterior != cmd.SalaId)
        {
            await _auditRepo.Registrar(AgendamentoSalaAudit.Registrar(
                agendamento.Id,
                agendamento.EstabelecimentoId,
                salaIdAnterior,
                cmd.SalaId,
                cmd.UsuarioSolicitanteId));
        }
    }
}
