namespace Imedto.Backend.Domain.Financeiro;

public interface ILancamentoRepository
{
    Task<Lancamento> ObterPorId(long id);
    Task<Lancamento?> ObterPorIdOuNulo(long id);
    Task Salvar(Lancamento lancamento);
}
