namespace Imedto.Backend.Domain.Financeiro;

public interface ILancamentoRepository
{
    Task<Lancamento> ObterPorId(long id);
    Task Salvar(Lancamento lancamento);
}
