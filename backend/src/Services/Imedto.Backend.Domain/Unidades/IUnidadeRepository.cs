namespace Imedto.Backend.Domain.Unidades;

public interface IUnidadeRepository
{
    Task<UnidadeEstabelecimento> ObterPorId(long id);
    Task<UnidadeEstabelecimento> ObterPorIdOuNulo(long id);
    Task<IReadOnlyList<UnidadeEstabelecimento>> ListarPorEstabelecimento(long estabelecimentoId);
    Task<UnidadeEstabelecimento> ObterPrincipalDoEstabelecimento(long estabelecimentoId);
    Task<bool> ExisteOutraComMesmoNome(long estabelecimentoId, string nome, long ignorarUnidadeId);
    Task Salvar(UnidadeEstabelecimento unidade);
    Task Excluir(UnidadeEstabelecimento unidade);
}
