using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Financeiro.Commands;

public class ReativarCategoriaFinanceiraCommandHandler : ICommandHandler<ReativarCategoriaFinanceiraCommand>
{
    private readonly ICategoriaFinanceiraRepository _repo;

    public ReativarCategoriaFinanceiraCommandHandler(ICategoriaFinanceiraRepository repo) => _repo = repo;

    public async Task Handle(ReativarCategoriaFinanceiraCommand cmd)
    {
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var categoria = await _repo.ObterPorIdOuNulo(cmd.CategoriaId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Categoria não encontrada.");

        categoria.Reativar();
        await _repo.Salvar(categoria);
    }
}
