namespace Imedto.Backend.Domain.Unidades;

public interface IUnidadeRepository
{
    /// <summary>
    /// Carrega a unidade filtrando por <paramref name="estabelecimentoId"/>
    /// (defense-in-depth IDOR/LGPD). Retorna null se inexistente ou de outro tenant.
    /// </summary>
    Task<UnidadeEstabelecimento?> ObterPorIdOuNulo(long id, long estabelecimentoId);

    Task<IReadOnlyList<UnidadeEstabelecimento>> ListarPorEstabelecimento(long estabelecimentoId);
    Task<UnidadeEstabelecimento> ObterPrincipalDoEstabelecimento(long estabelecimentoId);
    Task<bool> ExisteOutraComMesmoNome(long estabelecimentoId, string nome, long ignorarUnidadeId);
    Task Salvar(UnidadeEstabelecimento unidade);
    Task Excluir(UnidadeEstabelecimento unidade);
}
