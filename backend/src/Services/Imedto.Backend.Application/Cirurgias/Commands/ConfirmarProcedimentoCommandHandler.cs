using Imedto.Backend.Contracts.Cirurgias.Commands;
using Imedto.Backend.Domain.Cirurgias;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Cirurgias.Commands;

public class ConfirmarProcedimentoCommandHandler : ICommandHandler<ConfirmarProcedimentoCommand>
{
    private readonly IProcedimentoCirurgicoRepository _repo;
    private readonly IEventBus _events;
    private readonly IProntuarioAcessoLogService _acessoLog;

    public ConfirmarProcedimentoCommandHandler(
        IProcedimentoCirurgicoRepository repo,
        IEventBus events,
        IProntuarioAcessoLogService acessoLog)
    {
        _repo = repo;
        _events = events;
        _acessoLog = acessoLog;
    }

    public async Task Handle(ConfirmarProcedimentoCommand cmd)
    {
        var procedimento = await _repo.ObterPorId(cmd.ProcedimentoId);
        if (procedimento.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Procedimento não pertence a este estabelecimento.");

        procedimento.Confirmar();
        await _repo.Salvar(procedimento);

        if (cmd.SolicitanteUsuarioId != Guid.Empty)
        {
            await _acessoLog.RegistrarAsync(
                procedimento.ProntuarioId, cmd.SolicitanteUsuarioId, cmd.EstabelecimentoId,
                TipoAcessoProntuario.Escrita);
        }

        // Eventos disparam notificação para a equipe cirúrgica. O bus dispatcha pelo
        // runtime type, então `Publish(ev)` (com TEvent inferido como IDomainEvent) basta.
        foreach (var ev in procedimento.DomainEvents)
            await _events.Publish(ev);
        procedimento.ClearDomainEvents();
    }
}
