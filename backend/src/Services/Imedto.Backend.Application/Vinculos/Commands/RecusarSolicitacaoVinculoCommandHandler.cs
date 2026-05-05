using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Vinculos.Commands;

public class RecusarSolicitacaoVinculoCommandHandler : ICommandHandler<RecusarSolicitacaoVinculoCommand>
{
    private readonly ISolicitacaoVinculoRepository _solicitacaoRepo;
    private readonly IEstabelecimentoRepository _estabelecimentoRepo;
    private readonly IEventBus _eventBus;

    public RecusarSolicitacaoVinculoCommandHandler(
        ISolicitacaoVinculoRepository solicitacaoRepo,
        IEstabelecimentoRepository estabelecimentoRepo,
        IEventBus eventBus)
    {
        _solicitacaoRepo = solicitacaoRepo;
        _estabelecimentoRepo = estabelecimentoRepo;
        _eventBus = eventBus;
    }

    public async Task Handle(RecusarSolicitacaoVinculoCommand command)
    {
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var solicitacao = await _solicitacaoRepo.ObterPorIdNoEstabelecimentoOuNulo(command.SolicitacaoId, command.EstabelecimentoId)
            ?? throw new BusinessException("Solicitação não encontrada.");

        var estab = await _estabelecimentoRepo.ObterPorId(command.EstabelecimentoId);
        if (estab.DonoUsuarioId != command.RecusadoPorUsuarioId)
            throw new BusinessException("Apenas o dono do estabelecimento pode recusar solicitações.");

        solicitacao.Recusar(command.RecusadoPorUsuarioId, command.Motivo);
        await _solicitacaoRepo.Salvar(solicitacao);

        foreach (var evt in solicitacao.DomainEvents)
            await _eventBus.Publish(evt);

        solicitacao.ClearDomainEvents();
    }
}
