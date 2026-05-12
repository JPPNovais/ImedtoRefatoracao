namespace Imedto.Backend.Domain.Financeiro;

public interface IFormaPagamentoRepository
{
    /// <summary>
    /// Carrega a forma de pagamento filtrando por <paramref name="estabelecimentoId"/>
    /// (defense-in-depth IDOR/LGPD). Retorna null se inexistente ou de outro tenant.
    /// </summary>
    Task<FormaPagamento?> ObterPorIdOuNulo(long id, long estabelecimentoId);

    /// <summary>
    /// Indica se já existe forma de pagamento com mesmo nome no estabelecimento.
    /// Usado pelo handler para retornar 422 antes do INSERT — evita 500 da unique constraint.
    /// </summary>
    Task<bool> ExisteComNomeNoEstabelecimento(string nome, long estabelecimentoId);

    Task Salvar(FormaPagamento forma);
}
