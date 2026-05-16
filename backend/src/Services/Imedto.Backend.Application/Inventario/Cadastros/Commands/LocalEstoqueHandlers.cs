using Imedto.Backend.Contracts.Inventario.Cadastros.Commands;
using Imedto.Backend.Domain.Inventario.Cadastros;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Inventario.Cadastros.Commands;

public class CriarLocalEstoqueCommandHandler : ICommandHandler<CriarLocalEstoqueCommand>
{
    private readonly ILocalEstoqueRepository _repo;
    public CriarLocalEstoqueCommandHandler(ILocalEstoqueRepository repo) => _repo = repo;

    public async Task Handle(CriarLocalEstoqueCommand cmd)
    {
        if (!Enum.TryParse<TipoLocalEstoque>(cmd.Tipo, ignoreCase: false, out var tipo))
            throw new BusinessException("Tipo de local inválido.");

        if (await _repo.ExisteComNomeNoEstabelecimento(cmd.Nome, cmd.EstabelecimentoId))
            throw new BusinessException("Já existe um local com este nome.");

        var local = LocalEstoque.Criar(cmd.EstabelecimentoId, cmd.Nome, tipo, cmd.AndarSetor, cmd.Responsavel);
        await _repo.Salvar(local);
        cmd.LocalIdCriado = local.Id;
    }
}

public class AtualizarLocalEstoqueCommandHandler : ICommandHandler<AtualizarLocalEstoqueCommand>
{
    private readonly ILocalEstoqueRepository _repo;
    public AtualizarLocalEstoqueCommandHandler(ILocalEstoqueRepository repo) => _repo = repo;

    public async Task Handle(AtualizarLocalEstoqueCommand cmd)
    {
        if (!Enum.TryParse<TipoLocalEstoque>(cmd.Tipo, ignoreCase: false, out var tipo))
            throw new BusinessException("Tipo de local inválido.");

        var local = await _repo.ObterPorIdOuNulo(cmd.LocalId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Local não encontrado.");

        if (await _repo.ExisteComNomeNoEstabelecimento(cmd.Nome, cmd.EstabelecimentoId, ignorarId: cmd.LocalId))
            throw new BusinessException("Já existe um local com este nome.");

        local.Atualizar(cmd.Nome, tipo, cmd.AndarSetor, cmd.Responsavel);
        await _repo.Salvar(local);
    }
}

public class InativarLocalEstoqueCommandHandler : ICommandHandler<InativarLocalEstoqueCommand>
{
    private readonly ILocalEstoqueRepository _repo;
    public InativarLocalEstoqueCommandHandler(ILocalEstoqueRepository repo) => _repo = repo;

    public async Task Handle(InativarLocalEstoqueCommand cmd)
    {
        var local = await _repo.ObterPorIdOuNulo(cmd.LocalId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Local não encontrado.");

        if (await _repo.ExistemItensVinculados(cmd.LocalId, cmd.EstabelecimentoId))
            throw new BusinessException("Existem itens armazenados neste local. Reatribua antes de inativar.");

        local.Inativar();
        await _repo.Salvar(local);
    }
}

public class ReativarLocalEstoqueCommandHandler : ICommandHandler<ReativarLocalEstoqueCommand>
{
    private readonly ILocalEstoqueRepository _repo;
    public ReativarLocalEstoqueCommandHandler(ILocalEstoqueRepository repo) => _repo = repo;

    public async Task Handle(ReativarLocalEstoqueCommand cmd)
    {
        var local = await _repo.ObterPorIdOuNulo(cmd.LocalId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Local não encontrado.");
        local.Reativar();
        await _repo.Salvar(local);
    }
}
