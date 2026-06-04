using Imedto.Backend.Contracts.Inventario.Cadastros.Commands;
using Imedto.Backend.Domain.Inventario.Cadastros;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Inventario.Cadastros.Commands;

public class CriarFornecedorEstoqueCommandHandler : ICommandHandler<CriarFornecedorEstoqueCommand>
{
    private readonly IFornecedorEstoqueRepository _repo;
    public CriarFornecedorEstoqueCommandHandler(IFornecedorEstoqueRepository repo) => _repo = repo;

    public async Task Handle(CriarFornecedorEstoqueCommand cmd)
    {
        // O aggregate normaliza CNPJ — duplicidade é checada com dígitos crus.
        if (await _repo.ExisteComNomeNoEstabelecimento(cmd.RazaoSocial, cmd.EstabelecimentoId))
            throw new BusinessException("Já existe um fornecedor com esta razão social.");

        // Pré-valida CNPJ duplicado (se informado) — mensagem específica antes do INSERT.
        var cnpjDigitos = string.IsNullOrWhiteSpace(cmd.Cnpj)
            ? null
            : new string(cmd.Cnpj.Where(char.IsDigit).ToArray());
        if (!string.IsNullOrEmpty(cnpjDigitos) &&
            await _repo.ExisteComCnpjNoEstabelecimento(cnpjDigitos, cmd.EstabelecimentoId))
            throw new BusinessException("Já existe um fornecedor com este CNPJ.");

        var forn = FornecedorEstoque.Criar(
            cmd.EstabelecimentoId, cmd.RazaoSocial, cmd.NomeFantasia, cmd.Cnpj,
            cmd.ContatoNome, cmd.ContatoTelefone, cmd.ContatoEmail, cmd.PrazoEntregaDias,
            cmd.TipoPrazoEntrega);

        await _repo.Salvar(forn);
        cmd.FornecedorIdCriado = forn.Id;
    }
}

public class AtualizarFornecedorEstoqueCommandHandler : ICommandHandler<AtualizarFornecedorEstoqueCommand>
{
    private readonly IFornecedorEstoqueRepository _repo;
    public AtualizarFornecedorEstoqueCommandHandler(IFornecedorEstoqueRepository repo) => _repo = repo;

    public async Task Handle(AtualizarFornecedorEstoqueCommand cmd)
    {
        var forn = await _repo.ObterPorIdOuNulo(cmd.FornecedorId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Fornecedor não encontrado.");

        if (await _repo.ExisteComNomeNoEstabelecimento(cmd.RazaoSocial, cmd.EstabelecimentoId, ignorarId: cmd.FornecedorId))
            throw new BusinessException("Já existe um fornecedor com esta razão social.");

        var cnpjDigitos = string.IsNullOrWhiteSpace(cmd.Cnpj)
            ? null
            : new string(cmd.Cnpj.Where(char.IsDigit).ToArray());
        if (!string.IsNullOrEmpty(cnpjDigitos) &&
            await _repo.ExisteComCnpjNoEstabelecimento(cnpjDigitos, cmd.EstabelecimentoId, ignorarId: cmd.FornecedorId))
            throw new BusinessException("Já existe um fornecedor com este CNPJ.");

        forn.Atualizar(
            cmd.RazaoSocial, cmd.NomeFantasia, cmd.Cnpj,
            cmd.ContatoNome, cmd.ContatoTelefone, cmd.ContatoEmail, cmd.PrazoEntregaDias,
            cmd.TipoPrazoEntrega);
        await _repo.Salvar(forn);
    }
}

public class InativarFornecedorEstoqueCommandHandler : ICommandHandler<InativarFornecedorEstoqueCommand>
{
    private readonly IFornecedorEstoqueRepository _repo;
    public InativarFornecedorEstoqueCommandHandler(IFornecedorEstoqueRepository repo) => _repo = repo;

    public async Task Handle(InativarFornecedorEstoqueCommand cmd)
    {
        var forn = await _repo.ObterPorIdOuNulo(cmd.FornecedorId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Fornecedor não encontrado.");

        if (await _repo.ExistemItensVinculados(cmd.FornecedorId, cmd.EstabelecimentoId))
            throw new BusinessException("Existem itens com este fornecedor padrão. Reatribua antes de inativar.");

        forn.Inativar();
        await _repo.Salvar(forn);
    }
}

public class ReativarFornecedorEstoqueCommandHandler : ICommandHandler<ReativarFornecedorEstoqueCommand>
{
    private readonly IFornecedorEstoqueRepository _repo;
    public ReativarFornecedorEstoqueCommandHandler(IFornecedorEstoqueRepository repo) => _repo = repo;

    public async Task Handle(ReativarFornecedorEstoqueCommand cmd)
    {
        var forn = await _repo.ObterPorIdOuNulo(cmd.FornecedorId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Fornecedor não encontrado.");
        forn.Reativar();
        await _repo.Salvar(forn);
    }
}
