namespace Imedto.Backend.Domain.Orcamentos;

public interface IOrcamentoRepository
{
    Task<Orcamento> ObterPorId(long id);
    Task<Orcamento> ObterPorIdComItens(long id);
    Task Salvar(Orcamento orcamento);
}
