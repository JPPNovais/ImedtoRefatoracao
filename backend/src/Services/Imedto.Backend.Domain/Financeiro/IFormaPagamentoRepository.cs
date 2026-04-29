namespace Imedto.Backend.Domain.Financeiro;

public interface IFormaPagamentoRepository
{
    Task<FormaPagamento> ObterPorId(long id);
    Task<FormaPagamento?> ObterPorIdOuNulo(long id);
    Task Salvar(FormaPagamento forma);
}
