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
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var item = await _repo.ObterPorIdOuNulo(cmd.ItemId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Item não encontrado.");

        item.Atualizar(cmd.Nome, cmd.Categoria, cmd.UnidadeMedida, cmd.QuantidadeMinima);
        await _repo.Salvar(item);
    }
}
