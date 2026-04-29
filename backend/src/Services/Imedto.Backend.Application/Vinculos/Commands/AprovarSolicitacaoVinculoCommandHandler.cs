using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Vinculos.Commands;

/// <summary>
/// Aprovação pelo dono do estabelecimento. A criação do <c>VinculoProfissionalEstabelecimento</c>
/// é feita por <c>AoAprovarSolicitacaoCriarVinculoHandler</c> (event handler) — esta separação
/// mantém o aggregate puro (sem dependência cruzada de outro aggregate) e aproveita a
/// transação do <c>UnitOfWorkFilter</c>: tanto a solicitação aprovada quanto o vínculo são
/// commitados juntos.
/// </summary>
public class AprovarSolicitacaoVinculoCommandHandler : ICommandHandler<AprovarSolicitacaoVinculoCommand>
{
    private readonly ISolicitacaoVinculoRepository _solicitacaoRepo;
    private readonly IEstabelecimentoRepository _estabelecimentoRepo;
    private readonly IEventBus _eventBus;

    public AprovarSolicitacaoVinculoCommandHandler(
        ISolicitacaoVinculoRepository solicitacaoRepo,
        IEstabelecimentoRepository estabelecimentoRepo,
        IEventBus eventBus)
    {
        _solicitacaoRepo = solicitacaoRepo;
        _estabelecimentoRepo = estabelecimentoRepo;
        _eventBus = eventBus;
    }

    public async Task Handle(AprovarSolicitacaoVinculoCommand command)
    {
        var solicitacao = await _solicitacaoRepo.ObterPorId(command.SolicitacaoId);

        if (solicitacao.EstabelecimentoId != command.EstabelecimentoId)
            throw new BusinessException("Solicitação não pertence a este estabelecimento.");

        var estab = await _estabelecimentoRepo.ObterPorId(command.EstabelecimentoId);
        if (estab.DonoUsuarioId != command.AprovadoPorUsuarioId)
            throw new BusinessException("Apenas o dono do estabelecimento pode aprovar solicitações.");

        solicitacao.Aprovar(command.AprovadoPorUsuarioId);
        await _solicitacaoRepo.Salvar(solicitacao);

        foreach (var evt in solicitacao.DomainEvents)
            await _eventBus.Publish(evt);

        solicitacao.ClearDomainEvents();
    }
}
