using Imedto.Backend.Contracts.Inventario.Cadastros.Queries;
using Imedto.Backend.Contracts.Inventario.Cadastros.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories.Cadastros;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Inventario.Cadastros.Queries;

public class ListarCategoriasEstoqueQueryHandlers : IRequestHandler<ListarCategoriasEstoqueQuery, PaginaCategoriasEstoqueDto>
{
    private readonly CadastrosEstoqueQueryRepository _repo;
    public ListarCategoriasEstoqueQueryHandlers(CadastrosEstoqueQueryRepository repo) => _repo = repo;

    public Task<PaginaCategoriasEstoqueDto> Handle(ListarCategoriasEstoqueQuery q)
        => _repo.ListarCategorias(q.EstabelecimentoId, q.Busca, q.ApenasAtivos, q.Pagina, q.TamanhoPagina);
}

public class ListarFabricantesEstoqueQueryHandlers : IRequestHandler<ListarFabricantesEstoqueQuery, PaginaFabricantesEstoqueDto>
{
    private readonly CadastrosEstoqueQueryRepository _repo;
    public ListarFabricantesEstoqueQueryHandlers(CadastrosEstoqueQueryRepository repo) => _repo = repo;

    public Task<PaginaFabricantesEstoqueDto> Handle(ListarFabricantesEstoqueQuery q)
        => _repo.ListarFabricantes(q.EstabelecimentoId, q.Busca, q.ApenasAtivos, q.Pagina, q.TamanhoPagina);
}

public class ListarFornecedoresEstoqueQueryHandlers : IRequestHandler<ListarFornecedoresEstoqueQuery, PaginaFornecedoresEstoqueDto>
{
    private readonly CadastrosEstoqueQueryRepository _repo;
    public ListarFornecedoresEstoqueQueryHandlers(CadastrosEstoqueQueryRepository repo) => _repo = repo;

    public Task<PaginaFornecedoresEstoqueDto> Handle(ListarFornecedoresEstoqueQuery q)
        => _repo.ListarFornecedores(q.EstabelecimentoId, q.Busca, q.ApenasAtivos, q.Pagina, q.TamanhoPagina);
}

public class ListarLocaisEstoqueQueryHandlers : IRequestHandler<ListarLocaisEstoqueQuery, PaginaLocaisEstoqueDto>
{
    private readonly CadastrosEstoqueQueryRepository _repo;
    public ListarLocaisEstoqueQueryHandlers(CadastrosEstoqueQueryRepository repo) => _repo = repo;

    public Task<PaginaLocaisEstoqueDto> Handle(ListarLocaisEstoqueQuery q)
        => _repo.ListarLocais(q.EstabelecimentoId, q.Busca, q.ApenasAtivos, q.Pagina, q.TamanhoPagina);
}
