namespace Imedto.Backend.Domain.Inventario;

public interface IMovimentacaoEstoqueRepository
{
    Task Salvar(MovimentacaoEstoque movimentacao);
}
