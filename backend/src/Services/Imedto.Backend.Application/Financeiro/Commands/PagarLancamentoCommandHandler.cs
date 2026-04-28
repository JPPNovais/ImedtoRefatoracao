using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Financeiro.Commands;

public class PagarLancamentoCommandHandler : ICommandHandler<PagarLancamentoCommand>
{
    private readonly ILancamentoRepository _repo;
    private readonly IEventBus _events;

    public PagarLancamentoCommandHandler(ILancamentoRepository repo, IEventBus events)
    {
        _repo = repo;
        _events = events;
    }

    public async Task Handle(PagarLancamentoCommand cmd)
    {
        var lancamento = await _repo.ObterPorId(cmd.LancamentoId);

        if (lancamento.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Lançamento não encontrado neste estabelecimento.");

        lancamento.Pagar(cmd.DataPagamento);
        await _repo.Salvar(lancamento);

        foreach (var ev in lancamento.DomainEvents)
            await _events.Publish(ev);
        lancamento.ClearDomainEvents();
    }
}
