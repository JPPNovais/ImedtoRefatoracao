namespace Imedto.Backend.Domain.Inventario.Cadastros;

public interface ICategoriaEstoqueRepository
{
    Task<CategoriaEstoque?> ObterPorIdOuNulo(long id, long estabelecimentoId);
    /// <summary>Verifica duplicidade (case-insensitive) por (estabelecimento, nome), ignorando opcionalmente um id.</summary>
    Task<bool> ExisteComNomeNoEstabelecimento(string nome, long estabelecimentoId, long? ignorarId = null);
    /// <summary>Indica se existem itens de inventário (ativos ou não) ligados a esta categoria — usado para bloquear inativação.</summary>
    Task<bool> ExistemItensVinculados(long categoriaId, long estabelecimentoId);
    Task Salvar(CategoriaEstoque categoria);
    Task<CategoriaEstoque?> ObterPorNomeOuNulo(string nome, long estabelecimentoId);
}

public interface IFabricanteEstoqueRepository
{
    Task<FabricanteEstoque?> ObterPorIdOuNulo(long id, long estabelecimentoId);
    Task<bool> ExisteComNomeNoEstabelecimento(string nome, long estabelecimentoId, long? ignorarId = null);
    Task<bool> ExistemItensVinculados(long fabricanteId, long estabelecimentoId);
    Task Salvar(FabricanteEstoque fabricante);
    Task<FabricanteEstoque?> ObterPorNomeOuNulo(string nome, long estabelecimentoId);
}

public interface IFornecedorEstoqueRepository
{
    Task<FornecedorEstoque?> ObterPorIdOuNulo(long id, long estabelecimentoId);
    Task<bool> ExisteComNomeNoEstabelecimento(string razaoSocial, long estabelecimentoId, long? ignorarId = null);
    Task<bool> ExisteComCnpjNoEstabelecimento(string cnpj, long estabelecimentoId, long? ignorarId = null);
    Task<bool> ExistemItensVinculados(long fornecedorId, long estabelecimentoId);
    Task Salvar(FornecedorEstoque fornecedor);
    Task<FornecedorEstoque?> ObterPorCnpjOuNulo(string cnpjDigitos, long estabelecimentoId);
    Task<FornecedorEstoque?> ObterPorNomeOuNulo(string razaoSocial, long estabelecimentoId);
}

public interface ILocalEstoqueRepository
{
    Task<LocalEstoque?> ObterPorIdOuNulo(long id, long estabelecimentoId);
    Task<bool> ExisteComNomeNoEstabelecimento(string nome, long estabelecimentoId, long? ignorarId = null);
    Task<bool> ExistemItensVinculados(long localId, long estabelecimentoId);
    Task Salvar(LocalEstoque local);
    Task<LocalEstoque?> ObterPorNomeOuNulo(string nome, long estabelecimentoId);
}
