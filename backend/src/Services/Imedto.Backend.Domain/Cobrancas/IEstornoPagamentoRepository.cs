namespace Imedto.Backend.Domain.Cobrancas;

public interface IEstornoPagamentoRepository
{
    Task Salvar(EstornoPagamento estorno);
}
