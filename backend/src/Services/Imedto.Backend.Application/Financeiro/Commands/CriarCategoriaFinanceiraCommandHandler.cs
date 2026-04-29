using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Financeiro.Commands;

public class CriarCategoriaFinanceiraCommandHandler : ICommandHandler<CriarCategoriaFinanceiraCommand>
{
    private readonly ICategoriaFinanceiraRepository _repo;

    public CriarCategoriaFinanceiraCommandHandler(ICategoriaFinanceiraRepository repo) => _repo = repo;

    public async Task Handle(CriarCategoriaFinanceiraCommand cmd)
    {
        if (!Enum.TryParse<TipoCategoria>(cmd.Tipo, out var tipo))
            throw new BusinessException("Tipo inválido. Use 'Receita' ou 'Despesa'.");

        var categoria = CategoriaFinanceira.Criar(cmd.EstabelecimentoId, cmd.Nome, tipo);
        await _repo.Salvar(categoria);
    }
}
