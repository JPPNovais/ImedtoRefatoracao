using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Financeiro.Commands;

public class InativarCategoriaFinanceiraCommandHandler : ICommandHandler<InativarCategoriaFinanceiraCommand>
{
    private readonly ICategoriaFinanceiraRepository _repo;

    public InativarCategoriaFinanceiraCommandHandler(ICategoriaFinanceiraRepository repo) => _repo = repo;

    public async Task Handle(InativarCategoriaFinanceiraCommand cmd)
    {
        var categoria = await _repo.ObterPorId(cmd.CategoriaId);

        if (categoria.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Categoria não encontrada neste estabelecimento.");

        categoria.Inativar();
        await _repo.Salvar(categoria);
    }
}
