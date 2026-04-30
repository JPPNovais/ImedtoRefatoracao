namespace Imedto.Backend.Domain.Orcamentos.Catalogos;

public interface ICatalogoCirurgiaRepository
{
    Task<CatalogoCirurgia?> ObterPorIdOuNulo(long id);
    Task Salvar(CatalogoCirurgia entity);
    Task Remover(CatalogoCirurgia entity);
}

public interface IValorProfissionalOrcamentoRepository
{
    Task<ValorProfissionalOrcamento?> ObterPorIdOuNulo(long id);
    Task Salvar(ValorProfissionalOrcamento entity);
    Task Remover(ValorProfissionalOrcamento entity);
}

public interface IConfiguracaoLocalCirurgiaRepository
{
    Task<ConfiguracaoLocalCirurgia?> ObterPorIdOuNulo(long id);
    Task<ConfiguracaoLocalCirurgia?> ObterPorEstabelecimentoETipo(long estabelecimentoId, TipoInternacao tipo);
    Task Salvar(ConfiguracaoLocalCirurgia entity);
}

public interface ICatalogoEquipeEspecializadaRepository
{
    Task<CatalogoEquipeEspecializada?> ObterPorIdOuNulo(long id);
    Task Salvar(CatalogoEquipeEspecializada entity);
    Task Remover(CatalogoEquipeEspecializada entity);
}

public interface ICatalogoImplanteRepository
{
    Task<CatalogoImplante?> ObterPorIdOuNulo(long id);
    Task Salvar(CatalogoImplante entity);
    Task Remover(CatalogoImplante entity);
}

public interface IConfiguracaoPagamentoCatalogoRepository
{
    Task<ConfiguracaoPagamentoCatalogo?> ObterPorIdOuNulo(long id);
    Task Salvar(ConfiguracaoPagamentoCatalogo entity);
    Task Remover(ConfiguracaoPagamentoCatalogo entity);
}

public interface ICatalogoProdutoRepository
{
    Task<CatalogoProduto?> ObterPorIdOuNulo(long id);
    Task Salvar(CatalogoProduto entity);
    Task Remover(CatalogoProduto entity);
}

public interface ICatalogoCirurgiaProdutoRepository
{
    Task<CatalogoCirurgiaProduto?> ObterPorIdOuNulo(long id);
    Task<CatalogoCirurgiaProduto?> ObterPorCirurgiaProduto(long catalogoCirurgiaId, long catalogoProdutoId);
    Task<IReadOnlyList<CatalogoCirurgiaProduto>> ListarDaCirurgia(long catalogoCirurgiaId);
    Task Salvar(CatalogoCirurgiaProduto entity);
    Task Remover(CatalogoCirurgiaProduto entity);
}
