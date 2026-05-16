namespace Imedto.Backend.Domain.Inventario.Cadastros;

public interface ICategoriaEstoqueRepository
{
    Task<CategoriaEstoque?> ObterPorIdOuNulo(long id, long estabelecimentoId);
    /// <summary>Verifica duplicidade (case-insensitive) por (estabelecimento, nome), ignorando opcionalmente um id.</summary>
    Task<bool> ExisteComNomeNoEstabelecimento(string nome, long estabelecimentoId, long? ignorarId = null);
    /// <summary>Indica se existem itens de inventário (ativos ou não) ligados a esta categoria — usado para bloquear inativação.</summary>
    Task<bool> ExistemItensVinculados(long categoriaId, long estabelecimentoId);
    Task Salvar(CategoriaEstoque categoria);
}

public interface IFabricanteEstoqueRepository
{
    Task<FabricanteEstoque?> ObterPorIdOuNulo(long id, long estabelecimentoId);
    Task<bool> ExisteComNomeNoEstabelecimento(string nome, long estabelecimentoId, long? ignorarId = null);
    Task<bool> ExistemItensVinculados(long fabricanteId, long estabelecimentoId);
    Task Salvar(FabricanteEstoque fabricante);
}

public interface IFornecedorEstoqueRepository
{
    Task<FornecedorEstoque?> ObterPorIdOuNulo(long id, long estabelecimentoId);
    Task<bool> ExisteComNomeNoEstabelecimento(string razaoSocial, long estabelecimentoId, long? ignorarId = null);
    Task<bool> ExisteComCnpjNoEstabelecimento(string cnpj, long estabelecimentoId, long? ignorarId = null);
    Task<bool> ExistemItensVinculados(long fornecedorId, long estabelecimentoId);
    Task Salvar(FornecedorEstoque fornecedor);
}

public interface ILocalEstoqueRepository
{
    Task<LocalEstoque?> ObterPorIdOuNulo(long id, long estabelecimentoId);
    Task<bool> ExisteComNomeNoEstabelecimento(string nome, long estabelecimentoId, long? ignorarId = null);
    Task<bool> ExistemItensVinculados(long localId, long estabelecimentoId);
    Task Salvar(LocalEstoque local);
}
