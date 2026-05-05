using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Financeiro.Commands;

public class AtualizarCategoriaFinanceiraCommandHandler : ICommandHandler<AtualizarCategoriaFinanceiraCommand>
{
    private readonly ICategoriaFinanceiraRepository _repo;

    public AtualizarCategoriaFinanceiraCommandHandler(ICategoriaFinanceiraRepository repo) => _repo = repo;

    public async Task Handle(AtualizarCategoriaFinanceiraCommand cmd)
    {
        if (!Enum.TryParse<TipoCategoria>(cmd.Tipo, out var tipo))
            throw new BusinessException("Tipo inválido. Use 'Receita' ou 'Despesa'.");

        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var categoria = await _repo.ObterPorIdOuNulo(cmd.CategoriaId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Categoria não encontrada.");

        categoria.Atualizar(cmd.Nome, tipo);
        await _repo.Salvar(categoria);
    }
}
