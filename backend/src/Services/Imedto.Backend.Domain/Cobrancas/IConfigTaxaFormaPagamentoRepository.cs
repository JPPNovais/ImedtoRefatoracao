namespace Imedto.Backend.Domain.Cobrancas;

public interface IConfigTaxaFormaPagamentoRepository
{
    Task<ConfigTaxaFormaPagamento?> ObterPorIdOuNulo(long id, long estabelecimentoId);

    /// <summary>
    /// Retorna a taxa ativa para a forma de pagamento no estabelecimento. Null = sem config.
    /// </summary>
    Task<ConfigTaxaFormaPagamento?> ObterPorForma(long estabelecimentoId, long formaPagamentoId);

    Task Salvar(ConfigTaxaFormaPagamento config);
}
