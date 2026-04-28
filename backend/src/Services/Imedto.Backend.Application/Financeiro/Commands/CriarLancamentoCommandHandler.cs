using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Financeiro.Commands;

public class CriarLancamentoCommandHandler : ICommandHandler<CriarLancamentoCommand>
{
    private readonly ILancamentoRepository _repo;
    private readonly IEventBus _events;

    public CriarLancamentoCommandHandler(ILancamentoRepository repo, IEventBus events)
    {
        _repo = repo;
        _events = events;
    }

    public async Task Handle(CriarLancamentoCommand cmd)
    {
        if (!Enum.TryParse<TipoLancamento>(cmd.Tipo, out var tipo))
            throw new BusinessException("Tipo inválido. Use 'Receita' ou 'Despesa'.");

        var lancamento = Lancamento.Criar(
            cmd.EstabelecimentoId,
            tipo,
            cmd.Descricao,
            cmd.Valor,
            cmd.DataVencimento,
            cmd.Categoria,
            cmd.CriadoPorUsuarioId,
            cmd.OrcamentoId);

        await _repo.Salvar(lancamento);

        foreach (var ev in lancamento.DomainEvents)
            await _events.Publish(ev);
        lancamento.ClearDomainEvents();
    }
}
