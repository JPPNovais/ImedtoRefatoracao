using Imedto.Backend.Contracts.Inventario.Commands;
using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Inventario.Commands;

public class InativarItemInventarioCommandHandler : ICommandHandler<InativarItemInventarioCommand>
{
    private readonly IItemInventarioRepository _repo;
    private readonly IMovimentacaoEstoqueRepository _movRepo;

    public InativarItemInventarioCommandHandler(
        IItemInventarioRepository repo,
        IMovimentacaoEstoqueRepository movRepo)
    {
        _repo = repo;
        _movRepo = movRepo;
    }

    public async Task Handle(InativarItemInventarioCommand cmd)
    {
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var item = await _repo.ObterPorIdOuNulo(cmd.ItemId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Item não encontrado.");

        var movimentacao = item.Inativar(cmd.UsuarioId, cmd.Observacao);
        await _repo.Salvar(item);
        await _movRepo.Salvar(movimentacao);
    }
}
