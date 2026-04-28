using Imedto.Backend.Contracts.Inventario.Commands;
using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Inventario.Commands;

public class InativarItemInventarioCommandHandler : ICommandHandler<InativarItemInventarioCommand>
{
    private readonly IItemInventarioRepository _repo;

    public InativarItemInventarioCommandHandler(IItemInventarioRepository repo)
        => _repo = repo;

    public async Task Handle(InativarItemInventarioCommand cmd)
    {
        var item = await _repo.ObterPorId(cmd.ItemId);
        if (item.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Item não encontrado neste estabelecimento.");

        item.Inativar();
        await _repo.Salvar(item);
    }
}
