using Imedto.Backend.Contracts.Inventario.Commands;
using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.Domain.Inventario.Cadastros;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Inventario.Commands;

public class AtualizarItemInventarioCommandHandler : ICommandHandler<AtualizarItemInventarioCommand>
{
    private readonly IItemInventarioRepository _repo;
    private readonly ICategoriaEstoqueRepository _catRepo;
    private readonly IFabricanteEstoqueRepository _fabRepo;
    private readonly IFornecedorEstoqueRepository _fornRepo;
    private readonly ILocalEstoqueRepository _localRepo;

    public AtualizarItemInventarioCommandHandler(
        IItemInventarioRepository repo,
        ICategoriaEstoqueRepository catRepo,
        IFabricanteEstoqueRepository fabRepo,
        IFornecedorEstoqueRepository fornRepo,
        ILocalEstoqueRepository localRepo)
    {
        _repo = repo;
        _catRepo = catRepo;
        _fabRepo = fabRepo;
        _fornRepo = fornRepo;
        _localRepo = localRepo;
    }

    public async Task Handle(AtualizarItemInventarioCommand cmd)
    {
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var item = await _repo.ObterPorIdOuNulo(cmd.ItemId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Item não encontrado.");

        var categoria = await _catRepo.ObterPorIdOuNulo(cmd.CategoriaId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Categoria não encontrada.");
        if (!categoria.Ativo)
            throw new BusinessException("Categoria está inativa. Selecione uma categoria ativa.");

        if (cmd.FabricanteId.HasValue)
        {
            var fab = await _fabRepo.ObterPorIdOuNulo(cmd.FabricanteId.Value, cmd.EstabelecimentoId)
                ?? throw new BusinessException("Fabricante não encontrado.");
            if (!fab.Ativo) throw new BusinessException("Fabricante está inativo.");
        }
        if (cmd.FornecedorPadraoId.HasValue)
        {
            var forn = await _fornRepo.ObterPorIdOuNulo(cmd.FornecedorPadraoId.Value, cmd.EstabelecimentoId)
                ?? throw new BusinessException("Fornecedor não encontrado.");
            if (!forn.Ativo) throw new BusinessException("Fornecedor está inativo.");
        }
        if (cmd.LocalPadraoId.HasValue)
        {
            var local = await _localRepo.ObterPorIdOuNulo(cmd.LocalPadraoId.Value, cmd.EstabelecimentoId)
                ?? throw new BusinessException("Local não encontrado.");
            if (!local.Ativo) throw new BusinessException("Local está inativo.");
        }

        item.Atualizar(
            cmd.Nome,
            categoria.Id,
            categoria.Nome,
            cmd.UnidadeMedida,
            cmd.QuantidadeMinima,
            cmd.FabricanteId,
            cmd.FornecedorPadraoId,
            cmd.LocalPadraoId,
            cmd.CustoUnitario);
        await _repo.Salvar(item);
    }
}
