namespace Imedto.Backend.Domain.Financeiro;

public interface ICategoriaFinanceiraRepository
{
    Task<CategoriaFinanceira> ObterPorId(long id);
    Task<CategoriaFinanceira?> ObterPorIdOuNulo(long id);
    Task Salvar(CategoriaFinanceira categoria);
}
