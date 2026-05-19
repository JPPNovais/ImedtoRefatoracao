namespace Imedto.Backend.Domain.Orcamentos.Catalogos;

public interface ICatalogoCirurgiaRepository
{
    Task<CatalogoCirurgia?> ObterPorIdOuNulo(long id, long estabelecimentoId);
    Task Salvar(CatalogoCirurgia entity);
    Task Remover(CatalogoCirurgia entity);
}

public interface IValorProfissionalOrcamentoRepository
{
    Task<ValorProfissionalOrcamento?> ObterPorIdOuNulo(long id, long estabelecimentoId);
    Task Salvar(ValorProfissionalOrcamento entity);
    Task Remover(ValorProfissionalOrcamento entity);
}

public interface IConfiguracaoLocalCirurgiaRepository
{
    Task<ConfiguracaoLocalCirurgia?> ObterPorIdOuNulo(long id, long estabelecimentoId);
    Task<ConfiguracaoLocalCirurgia?> ObterPorEstabelecimentoETipo(long estabelecimentoId, TipoLocalCirurgia tipo);
    Task<IReadOnlyList<ConfiguracaoLocalCirurgia>> ListarDoEstabelecimento(long estabelecimentoId);
    Task Salvar(ConfiguracaoLocalCirurgia entity);
}

public interface ICatalogoEquipeEspecializadaRepository
{
    Task<CatalogoEquipeEspecializada?> ObterPorIdOuNulo(long id, long estabelecimentoId);
    Task Salvar(CatalogoEquipeEspecializada entity);
    Task Remover(CatalogoEquipeEspecializada entity);
}

public interface ICatalogoImplanteRepository
{
    Task<CatalogoImplante?> ObterPorIdOuNulo(long id, long estabelecimentoId);
    Task Salvar(CatalogoImplante entity);
    Task Remover(CatalogoImplante entity);
}

public interface IConfiguracaoPagamentoCatalogoRepository
{
    Task<ConfiguracaoPagamentoCatalogo?> ObterPorIdOuNulo(long id, long estabelecimentoId);
    Task Salvar(ConfiguracaoPagamentoCatalogo entity);
    Task Remover(ConfiguracaoPagamentoCatalogo entity);
}

public interface ICatalogoProdutoRepository
{
    Task<CatalogoProduto?> ObterPorIdOuNulo(long id, long estabelecimentoId);
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

public interface IOrcamentoTeamRoleRepository
{
    Task<OrcamentoTeamRole?> ObterPorIdOuNulo(long id, long estabelecimentoId);
    Task Salvar(OrcamentoTeamRole entity);
}

public interface IOrcamentoAnestesistaRepository
{
    Task<OrcamentoAnestesista?> ObterPorIdOuNulo(long id, long estabelecimentoId);
    Task<OrcamentoAnestesista?> ObterComFaixasOuNulo(long id, long estabelecimentoId);
    Task Salvar(OrcamentoAnestesista entity);
}

public interface IOrcamentoPacoteRepository
{
    Task<OrcamentoPacote?> ObterPorIdOuNulo(long id, long estabelecimentoId);
    Task<OrcamentoPacote?> ObterComAssociacoesOuNulo(long id, long estabelecimentoId);
    Task<bool> ExistePacoteAtivoComProcedimento(long catalogoCirurgiaId, long estabelecimentoId);
    Task Salvar(OrcamentoPacote entity);
}
