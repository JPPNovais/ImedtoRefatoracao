using Imedto.Backend.Contracts.Inventario.Cadastros.Commands;
using Imedto.Backend.Domain.Inventario.Cadastros;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Inventario.Cadastros.Commands;

public class CriarCategoriaEstoqueCommandHandler : ICommandHandler<CriarCategoriaEstoqueCommand>
{
    private readonly ICategoriaEstoqueRepository _repo;
    public CriarCategoriaEstoqueCommandHandler(ICategoriaEstoqueRepository repo) => _repo = repo;

    public async Task Handle(CriarCategoriaEstoqueCommand cmd)
    {
        // Pré-valida duplicidade (case-insensitive). Sem isso, a unique constraint
        // do DB vira 500 genérico em vez de 422 com mensagem útil.
        if (await _repo.ExisteComNomeNoEstabelecimento(cmd.Nome, cmd.EstabelecimentoId))
            throw new BusinessException("Já existe uma categoria com este nome.");

        var categoria = CategoriaEstoque.Criar(cmd.EstabelecimentoId, cmd.Nome, cmd.Cor, cmd.Icone);
        await _repo.Salvar(categoria);
        cmd.CategoriaIdCriada = categoria.Id;
    }
}

public class AtualizarCategoriaEstoqueCommandHandler : ICommandHandler<AtualizarCategoriaEstoqueCommand>
{
    private readonly ICategoriaEstoqueRepository _repo;
    public AtualizarCategoriaEstoqueCommandHandler(ICategoriaEstoqueRepository repo) => _repo = repo;

    public async Task Handle(AtualizarCategoriaEstoqueCommand cmd)
    {
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no repo.
        var categoria = await _repo.ObterPorIdOuNulo(cmd.CategoriaId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Categoria não encontrada.");

        if (await _repo.ExisteComNomeNoEstabelecimento(cmd.Nome, cmd.EstabelecimentoId, ignorarId: cmd.CategoriaId))
            throw new BusinessException("Já existe uma categoria com este nome.");

        categoria.Atualizar(cmd.Nome, cmd.Cor, cmd.Icone);
        await _repo.Salvar(categoria);
    }
}

public class InativarCategoriaEstoqueCommandHandler : ICommandHandler<InativarCategoriaEstoqueCommand>
{
    private readonly ICategoriaEstoqueRepository _repo;
    public InativarCategoriaEstoqueCommandHandler(ICategoriaEstoqueRepository repo) => _repo = repo;

    public async Task Handle(InativarCategoriaEstoqueCommand cmd)
    {
        var categoria = await _repo.ObterPorIdOuNulo(cmd.CategoriaId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Categoria não encontrada.");

        if (await _repo.ExistemItensVinculados(cmd.CategoriaId, cmd.EstabelecimentoId))
            throw new BusinessException("Inative os itens primeiro ou reatribua a categoria.");

        categoria.Inativar();
        await _repo.Salvar(categoria);
    }
}

public class ReativarCategoriaEstoqueCommandHandler : ICommandHandler<ReativarCategoriaEstoqueCommand>
{
    private readonly ICategoriaEstoqueRepository _repo;
    public ReativarCategoriaEstoqueCommandHandler(ICategoriaEstoqueRepository repo) => _repo = repo;

    public async Task Handle(ReativarCategoriaEstoqueCommand cmd)
    {
        var categoria = await _repo.ObterPorIdOuNulo(cmd.CategoriaId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Categoria não encontrada.");
        categoria.Reativar();
        await _repo.Salvar(categoria);
    }
}
