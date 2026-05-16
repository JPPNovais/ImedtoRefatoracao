using Imedto.Backend.Contracts.Inventario.Cadastros.Commands;
using Imedto.Backend.Domain.Inventario.Cadastros;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Inventario.Cadastros.Commands;

public class CriarFabricanteEstoqueCommandHandler : ICommandHandler<CriarFabricanteEstoqueCommand>
{
    private readonly IFabricanteEstoqueRepository _repo;
    public CriarFabricanteEstoqueCommandHandler(IFabricanteEstoqueRepository repo) => _repo = repo;

    public async Task Handle(CriarFabricanteEstoqueCommand cmd)
    {
        if (await _repo.ExisteComNomeNoEstabelecimento(cmd.Nome, cmd.EstabelecimentoId))
            throw new BusinessException("Já existe um fabricante com este nome.");

        var fab = FabricanteEstoque.Criar(cmd.EstabelecimentoId, cmd.Nome, cmd.Pais);
        await _repo.Salvar(fab);
        cmd.FabricanteIdCriado = fab.Id;
    }
}

public class AtualizarFabricanteEstoqueCommandHandler : ICommandHandler<AtualizarFabricanteEstoqueCommand>
{
    private readonly IFabricanteEstoqueRepository _repo;
    public AtualizarFabricanteEstoqueCommandHandler(IFabricanteEstoqueRepository repo) => _repo = repo;

    public async Task Handle(AtualizarFabricanteEstoqueCommand cmd)
    {
        var fab = await _repo.ObterPorIdOuNulo(cmd.FabricanteId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Fabricante não encontrado.");

        if (await _repo.ExisteComNomeNoEstabelecimento(cmd.Nome, cmd.EstabelecimentoId, ignorarId: cmd.FabricanteId))
            throw new BusinessException("Já existe um fabricante com este nome.");

        fab.Atualizar(cmd.Nome, cmd.Pais);
        await _repo.Salvar(fab);
    }
}

public class InativarFabricanteEstoqueCommandHandler : ICommandHandler<InativarFabricanteEstoqueCommand>
{
    private readonly IFabricanteEstoqueRepository _repo;
    public InativarFabricanteEstoqueCommandHandler(IFabricanteEstoqueRepository repo) => _repo = repo;

    public async Task Handle(InativarFabricanteEstoqueCommand cmd)
    {
        var fab = await _repo.ObterPorIdOuNulo(cmd.FabricanteId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Fabricante não encontrado.");

        if (await _repo.ExistemItensVinculados(cmd.FabricanteId, cmd.EstabelecimentoId))
            throw new BusinessException("Existem itens vinculados a este fabricante. Reatribua antes de inativar.");

        fab.Inativar();
        await _repo.Salvar(fab);
    }
}

public class ReativarFabricanteEstoqueCommandHandler : ICommandHandler<ReativarFabricanteEstoqueCommand>
{
    private readonly IFabricanteEstoqueRepository _repo;
    public ReativarFabricanteEstoqueCommandHandler(IFabricanteEstoqueRepository repo) => _repo = repo;

    public async Task Handle(ReativarFabricanteEstoqueCommand cmd)
    {
        var fab = await _repo.ObterPorIdOuNulo(cmd.FabricanteId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Fabricante não encontrado.");
        fab.Reativar();
        await _repo.Salvar(fab);
    }
}
