using Imedto.Backend.Contracts.Inventario.Commands;
using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Inventario.Commands;

public class RegistrarMovimentacaoEstoqueCommandHandler : ICommandHandler<RegistrarMovimentacaoEstoqueCommand>
{
    private readonly IItemInventarioRepository _repo;
    private readonly IMovimentacaoEstoqueRepository _movRepo;
    private readonly IEventBus _eventBus;

    public RegistrarMovimentacaoEstoqueCommandHandler(
        IItemInventarioRepository repo,
        IMovimentacaoEstoqueRepository movRepo,
        IEventBus eventBus)
    {
        _repo = repo;
        _movRepo = movRepo;
        _eventBus = eventBus;
    }

    public async Task Handle(RegistrarMovimentacaoEstoqueCommand cmd)
    {
        var item = await _repo.ObterPorId(cmd.ItemInventarioId);
        if (item.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Item não encontrado neste estabelecimento.");

        MovimentacaoEstoque mov;
        if (cmd.Tipo == nameof(TipoMovimentacaoEstoque.Entrada))
            mov = item.RegistrarEntrada(cmd.Quantidade, cmd.UsuarioId, cmd.Observacao);
        else if (cmd.Tipo == nameof(TipoMovimentacaoEstoque.Saida))
            mov = item.RegistrarSaida(cmd.Quantidade, cmd.UsuarioId, cmd.Observacao);
        else
            throw new BusinessException($"Tipo de movimentação inválido: '{cmd.Tipo}'.");

        await _repo.Salvar(item);
        await _movRepo.Salvar(mov);

        foreach (var ev in item.DomainEvents)
            await _eventBus.Publish(ev);
        item.ClearDomainEvents();
    }
}
