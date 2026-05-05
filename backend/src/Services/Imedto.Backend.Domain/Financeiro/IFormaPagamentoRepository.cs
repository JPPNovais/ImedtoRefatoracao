namespace Imedto.Backend.Domain.Financeiro;

public interface IFormaPagamentoRepository
{
    /// <summary>
    /// Carrega a forma de pagamento filtrando por <paramref name="estabelecimentoId"/>
    /// (defense-in-depth IDOR/LGPD). Retorna null se inexistente ou de outro tenant.
    /// </summary>
    Task<FormaPagamento?> ObterPorIdOuNulo(long id, long estabelecimentoId);

    Task Salvar(FormaPagamento forma);
}
