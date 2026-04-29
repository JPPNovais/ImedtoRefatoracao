using Imedto.Backend.Contracts.Inventario.Commands;
using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Inventario.Commands;

public class CriarItemInventarioCommandHandler : ICommandHandler<CriarItemInventarioCommand>
{
    private readonly IItemInventarioRepository _repo;
    private readonly IMovimentacaoEstoqueRepository _movRepo;
    private readonly IEventBus _eventBus;

    public CriarItemInventarioCommandHandler(
        IItemInventarioRepository repo,
        IMovimentacaoEstoqueRepository movRepo,
        IEventBus eventBus)
    {
        _repo = repo;
        _movRepo = movRepo;
        _eventBus = eventBus;
    }

    public async Task Handle(CriarItemInventarioCommand cmd)
    {
        var item = ItemInventario.Criar(
            cmd.EstabelecimentoId,
            cmd.Codigo,
            cmd.Nome,
            cmd.Categoria,
            cmd.UnidadeMedida,
            cmd.QuantidadeMinima);

        await _repo.Salvar(item);   // Id é populado pelo banco
        cmd.ItemIdCriado = item.Id;

        if (cmd.QuantidadeInicial > 0)
        {
            if (cmd.CustoUnitarioInicial <= 0)
                throw new BusinessException("Custo unitário inicial é obrigatório quando há quantidade inicial.");

            var mov = item.RegistrarEntrada(cmd.QuantidadeInicial, cmd.CriadoPorUsuarioId, cmd.CustoUnitarioInicial, "Estoque inicial");
            await _repo.Salvar(item);
            await _movRepo.Salvar(mov);
        }
    }
}
