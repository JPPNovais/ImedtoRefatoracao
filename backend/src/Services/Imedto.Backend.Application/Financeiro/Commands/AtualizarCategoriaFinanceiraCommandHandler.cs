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

        var categoria = await _repo.ObterPorIdOuNulo(cmd.CategoriaId)
            ?? throw new BusinessException("Categoria não encontrada.");
        // Mensagem padronizada (defense-in-depth: nao vaza existencia cross-tenant).
        if (categoria.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Categoria não encontrada.");

        categoria.Atualizar(cmd.Nome, tipo);
        await _repo.Salvar(categoria);
    }
}
