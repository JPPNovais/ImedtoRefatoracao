namespace Imedto.Backend.Domain.PedidosExame;

public interface IPedidoExameRepository
{
    Task<PedidoExame?> ObterPorIdOuNulo(long id, long estabelecimentoId);
    Task Salvar(PedidoExame pedido);
}
