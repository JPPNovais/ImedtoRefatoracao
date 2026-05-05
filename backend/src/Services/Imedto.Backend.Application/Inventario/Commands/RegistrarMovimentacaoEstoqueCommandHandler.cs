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
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var item = await _repo.ObterPorIdOuNulo(cmd.ItemInventarioId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Item não encontrado.");

        MovimentacaoEstoque mov;
        if (cmd.Tipo == nameof(TipoMovimentacaoEstoque.Entrada))
        {
            if (cmd.CustoUnitario <= 0)
                throw new BusinessException("Custo unitário é obrigatório e deve ser maior que zero em entradas.");
            mov = item.RegistrarEntrada(cmd.Quantidade, cmd.UsuarioId, cmd.CustoUnitario, cmd.Observacao);
        }
        else if (cmd.Tipo == nameof(TipoMovimentacaoEstoque.Saida))
        {
            // Saída: custo unitário do command é ignorado — agregado usa CustoMedio como snapshot.
            mov = item.RegistrarSaida(cmd.Quantidade, cmd.UsuarioId, cmd.Observacao);
        }
        else
            throw new BusinessException($"Tipo de movimentação inválido: '{cmd.Tipo}'.");

        await _repo.Salvar(item);
        await _movRepo.Salvar(mov);

        foreach (var ev in item.DomainEvents)
            await _eventBus.Publish(ev);
        item.ClearDomainEvents();
    }
}
