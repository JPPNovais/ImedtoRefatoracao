using Imedto.Backend.Contracts.Orcamentos.Commands;
using Imedto.Backend.Domain.Orcamentos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Orcamentos.Commands;

public class AprovarOrcamentoCommandHandler : ICommandHandler<AprovarOrcamentoCommand>
{
    private readonly IOrcamentoRepository _repo;
    private readonly IEventBus _events;

    public AprovarOrcamentoCommandHandler(IOrcamentoRepository repo, IEventBus events)
    {
        _repo = repo;
        _events = events;
    }

    public async Task Handle(AprovarOrcamentoCommand cmd)
    {
        var orcamento = await _repo.ObterPorIdCompleto(cmd.OrcamentoId);

        if (orcamento.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Orçamento não encontrado neste estabelecimento.");

        orcamento.Aprovar();
        await _repo.Salvar(orcamento);

        foreach (var ev in orcamento.DomainEvents)
            await _events.Publish(ev);
        orcamento.ClearDomainEvents();
    }
}
