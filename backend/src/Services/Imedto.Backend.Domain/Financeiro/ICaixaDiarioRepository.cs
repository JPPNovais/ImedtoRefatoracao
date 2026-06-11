namespace Imedto.Backend.Domain.Financeiro;

public interface ICaixaDiarioRepository
{
    Task<CaixaDiario?> ObterPorData(long estabelecimentoId, DateOnly data);
    Task Salvar(CaixaDiario caixa);
}
