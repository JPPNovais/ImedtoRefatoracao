using Imedto.Backend.Contracts.Inventario.Commands;
using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.Domain.Inventario.Cadastros;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Inventario.Commands;

public class CriarItemInventarioCommandHandler : ICommandHandler<CriarItemInventarioCommand>
{
    private readonly IItemInventarioRepository _repo;
    private readonly IMovimentacaoEstoqueRepository _movRepo;
    private readonly ICategoriaEstoqueRepository _catRepo;
    private readonly IFabricanteEstoqueRepository _fabRepo;
    private readonly IFornecedorEstoqueRepository _fornRepo;
    private readonly ILocalEstoqueRepository _localRepo;
    private readonly IEventBus _eventBus;

    public CriarItemInventarioCommandHandler(
        IItemInventarioRepository repo,
        IMovimentacaoEstoqueRepository movRepo,
        ICategoriaEstoqueRepository catRepo,
        IFabricanteEstoqueRepository fabRepo,
        IFornecedorEstoqueRepository fornRepo,
        ILocalEstoqueRepository localRepo,
        IEventBus eventBus)
    {
        _repo = repo;
        _movRepo = movRepo;
        _catRepo = catRepo;
        _fabRepo = fabRepo;
        _fornRepo = fornRepo;
        _localRepo = localRepo;
        _eventBus = eventBus;
    }

    public async Task Handle(CriarItemInventarioCommand cmd)
    {
        if (cmd.QuantidadeInicial < 0)
            throw new BusinessException("Quantidade inicial não pode ser negativa.");

        // FK obrigatória — Categoria precisa existir, ser ativa, e pertencer ao tenant.
        var categoria = await _catRepo.ObterPorIdOuNulo(cmd.CategoriaId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Categoria não encontrada.");
        if (!categoria.Ativo)
            throw new BusinessException("Categoria está inativa. Selecione uma categoria ativa.");

        // FKs opcionais — se vieram, validam: pertencem ao tenant + estão ativas.
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

        // Pré-valida unicidade do código no estabelecimento.
        if (await _repo.ExisteComCodigoNoEstabelecimento(cmd.Codigo, cmd.EstabelecimentoId))
            throw new BusinessException("Já existe um item com este código no estabelecimento.");

        var item = ItemInventario.Criar(
            cmd.EstabelecimentoId,
            cmd.Codigo,
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
        cmd.ItemIdCriado = item.Id;

        if (cmd.QuantidadeInicial > 0)
        {
            if (cmd.CustoUnitarioInicial <= 0)
                throw new BusinessException("Custo unitário inicial é obrigatório quando há quantidade inicial.");

            var mov = item.RegistrarEntrada(cmd.QuantidadeInicial, cmd.CriadoPorUsuarioId, cmd.CustoUnitarioInicial, "Estoque inicial");
            await _repo.Salvar(item);
            await _movRepo.Salvar(mov);
        }
    }
}
