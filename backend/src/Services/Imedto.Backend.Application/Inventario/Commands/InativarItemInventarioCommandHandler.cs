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
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var item = await _repo.ObterPorIdOuNulo(cmd.ItemId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Item não encontrado.");

        item.Inativar();
        await _repo.Salvar(item);
    }
}
