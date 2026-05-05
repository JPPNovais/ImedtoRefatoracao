namespace Imedto.Backend.Domain.Financeiro;

public interface ILancamentoRepository
{
    /// <summary>
    /// Carrega o lançamento filtrando por <paramref name="estabelecimentoId"/>
    /// (defense-in-depth IDOR/LGPD). Retorna null se inexistente ou de outro tenant.
    /// </summary>
    Task<Lancamento?> ObterPorIdOuNulo(long id, long estabelecimentoId);

    Task Salvar(Lancamento lancamento);
}
