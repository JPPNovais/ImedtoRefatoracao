using Imedto.Backend.Contracts.Orcamentos.Catalogos.Queries;
using Imedto.Backend.Contracts.Orcamentos.Catalogos.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories.OrcamentoCatalogos;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Orcamentos.Catalogos;

public class ListarCatalogoCirurgiasQueryHandlers : IRequestHandler<ListarCatalogoCirurgiasQuery, IEnumerable<CatalogoCirurgiaDto>>
{
    private readonly OrcamentoCatalogoQueryRepository _repo;
    public ListarCatalogoCirurgiasQueryHandlers(OrcamentoCatalogoQueryRepository repo) => _repo = repo;
    public Task<IEnumerable<CatalogoCirurgiaDto>> Handle(ListarCatalogoCirurgiasQuery q)
        => _repo.ListarCirurgias(q.EstabelecimentoId, q.Ativas);
}

public class ListarValoresProfissionalQueryHandlers : IRequestHandler<ListarValoresProfissionalQuery, IEnumerable<ValorProfissionalOrcamentoDto>>
{
    private readonly OrcamentoCatalogoQueryRepository _repo;
    public ListarValoresProfissionalQueryHandlers(OrcamentoCatalogoQueryRepository repo) => _repo = repo;
    public Task<IEnumerable<ValorProfissionalOrcamentoDto>> Handle(ListarValoresProfissionalQuery q)
        => _repo.ListarValoresProfissional(q.EstabelecimentoId, q.Ativos);
}

public class ListarConfiguracoesLocalQueryHandlers : IRequestHandler<ListarConfiguracoesLocalQuery, IEnumerable<ConfiguracaoLocalCirurgiaDto>>
{
    private readonly OrcamentoCatalogoQueryRepository _repo;
    public ListarConfiguracoesLocalQueryHandlers(OrcamentoCatalogoQueryRepository repo) => _repo = repo;
    public Task<IEnumerable<ConfiguracaoLocalCirurgiaDto>> Handle(ListarConfiguracoesLocalQuery q)
        => _repo.ListarConfiguracoesLocal(q.EstabelecimentoId);
}

public class ListarCatalogoEquipesQueryHandlers : IRequestHandler<ListarCatalogoEquipesQuery, IEnumerable<CatalogoEquipeEspecializadaDto>>
{
    private readonly OrcamentoCatalogoQueryRepository _repo;
    public ListarCatalogoEquipesQueryHandlers(OrcamentoCatalogoQueryRepository repo) => _repo = repo;
    public Task<IEnumerable<CatalogoEquipeEspecializadaDto>> Handle(ListarCatalogoEquipesQuery q)
        => _repo.ListarEquipes(q.EstabelecimentoId, q.Ativas);
}

public class ListarCatalogoImplantesQueryHandlers : IRequestHandler<ListarCatalogoImplantesQuery, IEnumerable<CatalogoImplanteDto>>
{
    private readonly OrcamentoCatalogoQueryRepository _repo;
    public ListarCatalogoImplantesQueryHandlers(OrcamentoCatalogoQueryRepository repo) => _repo = repo;
    public Task<IEnumerable<CatalogoImplanteDto>> Handle(ListarCatalogoImplantesQuery q)
        => _repo.ListarImplantes(q.EstabelecimentoId, q.Ativos);
}

public class ListarCatalogoProdutosQueryHandlers : IRequestHandler<ListarCatalogoProdutosQuery, IEnumerable<CatalogoProdutoDto>>
{
    private readonly OrcamentoCatalogoQueryRepository _repo;
    public ListarCatalogoProdutosQueryHandlers(OrcamentoCatalogoQueryRepository repo) => _repo = repo;
    public Task<IEnumerable<CatalogoProdutoDto>> Handle(ListarCatalogoProdutosQuery q)
        => _repo.ListarProdutos(q.EstabelecimentoId, q.Ativos);
}

public class ListarProdutosDaCirurgiaQueryHandlers : IRequestHandler<ListarProdutosDaCirurgiaQuery, IEnumerable<CatalogoCirurgiaProdutoDto>>
{
    private readonly OrcamentoCatalogoQueryRepository _repo;
    public ListarProdutosDaCirurgiaQueryHandlers(OrcamentoCatalogoQueryRepository repo) => _repo = repo;
    public Task<IEnumerable<CatalogoCirurgiaProdutoDto>> Handle(ListarProdutosDaCirurgiaQuery q)
        => _repo.ListarProdutosDaCirurgia(q.CatalogoCirurgiaId, q.EstabelecimentoId);
}

public class ListarConfiguracoesPagamentoQueryHandlers : IRequestHandler<ListarConfiguracoesPagamentoQuery, IEnumerable<ConfiguracaoPagamentoCatalogoDto>>
{
    private readonly OrcamentoCatalogoQueryRepository _repo;
    public ListarConfiguracoesPagamentoQueryHandlers(OrcamentoCatalogoQueryRepository repo) => _repo = repo;
    public Task<IEnumerable<ConfiguracaoPagamentoCatalogoDto>> Handle(ListarConfiguracoesPagamentoQuery q)
        => _repo.ListarConfiguracoesPagamento(q.EstabelecimentoId, q.Ativas);
}

public class ListarOrcamentoTeamRolesQueryHandlers : IRequestHandler<ListarOrcamentoTeamRolesQuery, IEnumerable<OrcamentoTeamRoleDto>>
{
    private readonly OrcamentoCatalogoQueryRepository _repo;
    public ListarOrcamentoTeamRolesQueryHandlers(OrcamentoCatalogoQueryRepository repo) => _repo = repo;
    public Task<IEnumerable<OrcamentoTeamRoleDto>> Handle(ListarOrcamentoTeamRolesQuery q)
        => _repo.ListarTeamRoles(q.EstabelecimentoId, q.Ativos);
}

public class ListarOrcamentoAnestesistasQueryHandlers : IRequestHandler<ListarOrcamentoAnestesistasQuery, IEnumerable<OrcamentoAnestesistaDto>>
{
    private readonly OrcamentoCatalogoQueryRepository _repo;
    public ListarOrcamentoAnestesistasQueryHandlers(OrcamentoCatalogoQueryRepository repo) => _repo = repo;
    public Task<IEnumerable<OrcamentoAnestesistaDto>> Handle(ListarOrcamentoAnestesistasQuery q)
        => _repo.ListarAnestesistas(q.EstabelecimentoId, q.Ativos);
}

public class ObterOrcamentoAnestesistaQueryHandlers : IRequestHandler<ObterOrcamentoAnestesistaQuery, OrcamentoAnestesistaDto?>
{
    private readonly OrcamentoCatalogoQueryRepository _repo;
    public ObterOrcamentoAnestesistaQueryHandlers(OrcamentoCatalogoQueryRepository repo) => _repo = repo;
    public Task<OrcamentoAnestesistaDto?> Handle(ObterOrcamentoAnestesistaQuery q)
        => _repo.ObterAnestesista(q.Id, q.EstabelecimentoId);
}

public class ListarOrcamentoPacotesQueryHandlers : IRequestHandler<ListarOrcamentoPacotesQuery, IEnumerable<OrcamentoPacoteResumoDto>>
{
    private readonly OrcamentoCatalogoQueryRepository _repo;
    public ListarOrcamentoPacotesQueryHandlers(OrcamentoCatalogoQueryRepository repo) => _repo = repo;
    public Task<IEnumerable<OrcamentoPacoteResumoDto>> Handle(ListarOrcamentoPacotesQuery q)
        => _repo.ListarPacotes(q.EstabelecimentoId, q.Ativos);
}

public class ObterOrcamentoPacoteQueryHandlers : IRequestHandler<ObterOrcamentoPacoteQuery, OrcamentoPacoteDetalheDto?>
{
    private readonly OrcamentoCatalogoQueryRepository _repo;
    public ObterOrcamentoPacoteQueryHandlers(OrcamentoCatalogoQueryRepository repo) => _repo = repo;
    public Task<OrcamentoPacoteDetalheDto?> Handle(ObterOrcamentoPacoteQuery q)
        => _repo.ObterPacote(q.Id, q.EstabelecimentoId);
}
