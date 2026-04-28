using Imedto.Backend.Contracts.Inventario.Commands;
using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Inventario.Commands;

public class AtualizarItemInventarioCommandHandler : ICommandHandler<AtualizarItemInventarioCommand>
{
    private readonly IItemInventarioRepository _repo;

    public AtualizarItemInventarioCommandHandler(IItemInventarioRepository repo)
        => _repo = repo;

    public async Task Handle(AtualizarItemInventarioCommand cmd)
    {
        var item = await _repo.ObterPorId(cmd.ItemId);
        if (item.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Item não encontrado neste estabelecimento.");

        item.Atualizar(cmd.Nome, cmd.Categoria, cmd.UnidadeMedida, cmd.QuantidadeMinima);
        await _repo.Salvar(item);
    }
}
