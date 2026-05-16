using Imedto.Backend.Contracts.Orcamentos.Catalogos.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Orcamentos.Catalogos.Queries;

public class ListarCatalogoCirurgiasQuery : IQuery<IEnumerable<CatalogoCirurgiaDto>>
{
    public long EstabelecimentoId { get; set; }
    public bool? Ativas { get; set; }
}

public class ListarValoresProfissionalQuery : IQuery<IEnumerable<ValorProfissionalOrcamentoDto>>
{
    public long EstabelecimentoId { get; set; }
    public bool? Ativos { get; set; }
}

public class ListarConfiguracoesLocalQuery : IQuery<IEnumerable<ConfiguracaoLocalCirurgiaDto>>
{
    public long EstabelecimentoId { get; set; }
}

public class ListarCatalogoEquipesQuery : IQuery<IEnumerable<CatalogoEquipeEspecializadaDto>>
{
    public long EstabelecimentoId { get; set; }
    public bool? Ativas { get; set; }
}

public class ListarCatalogoImplantesQuery : IQuery<IEnumerable<CatalogoImplanteDto>>
{
    public long EstabelecimentoId { get; set; }
    public bool? Ativos { get; set; }
}

public class ListarConfiguracoesPagamentoQuery : IQuery<IEnumerable<ConfiguracaoPagamentoCatalogoDto>>
{
    public long EstabelecimentoId { get; set; }
    public bool? Ativas { get; set; }
}

public class ListarCatalogoProdutosQuery : IQuery<IEnumerable<CatalogoProdutoDto>>
{
    public long EstabelecimentoId { get; set; }
    public bool? Ativos { get; set; }
}

public class ListarProdutosDaCirurgiaQuery : IQuery<IEnumerable<CatalogoCirurgiaProdutoDto>>
{
    public long CatalogoCirurgiaId { get; set; }
    public long EstabelecimentoId { get; set; }
}

public class ListarOrcamentoTeamRolesQuery : IQuery<IEnumerable<OrcamentoTeamRoleDto>>
{
    public long EstabelecimentoId { get; set; }
    public bool? Ativos { get; set; }
}

public class ListarOrcamentoAnestesistasQuery : IQuery<IEnumerable<OrcamentoAnestesistaDto>>
{
    public long EstabelecimentoId { get; set; }
    public bool? Ativos { get; set; }
}

public class ObterOrcamentoAnestesistaQuery : IQuery<OrcamentoAnestesistaDto?>
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
}

public class ListarOrcamentoPacotesQuery : IQuery<IEnumerable<OrcamentoPacoteResumoDto>>
{
    public long EstabelecimentoId { get; set; }
    public bool? Ativos { get; set; }
}

public class ObterOrcamentoPacoteQuery : IQuery<OrcamentoPacoteDetalheDto?>
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
}
