using Imedto.Backend.Contracts.Termos.Commands;
using Imedto.Backend.Domain.Termos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Termos.Commands;

public sealed class RevogarTermoCommandHandler : ICommandHandler<RevogarTermoCommand>
{
    private readonly ITermoEmitidoRepository _repo;
    private readonly ITermoAuditLogger _audit;
    private readonly IEventBus _eventBus;

    public RevogarTermoCommandHandler(
        ITermoEmitidoRepository repo,
        ITermoAuditLogger audit,
        IEventBus eventBus)
    {
        _repo = repo;
        _audit = audit;
        _eventBus = eventBus;
    }

    public async Task Handle(RevogarTermoCommand cmd)
    {
        var termo = await _repo.ObterPorIdOuNulo(cmd.TermoEmitidoId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Termo não encontrado.");

        termo.Revogar(cmd.SolicitanteUsuarioId, cmd.Motivo);
        await _repo.Salvar(termo);

        foreach (var ev in termo.DomainEvents)
            await _eventBus.Publish(ev);
        termo.ClearDomainEvents();

        await _audit.RegistrarAsync(
            cmd.EstabelecimentoId, cmd.SolicitanteUsuarioId,
            "termo-revogado", "TermoEmitido", termo.Id);
    }
}
