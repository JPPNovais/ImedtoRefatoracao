namespace Imedto.Backend.Domain.Automacoes;

public interface IRegraAutomacaoRepository
{
    Task<RegraAutomacao?> ObterPorIdOuNulo(long id);

    /// <summary>
    /// Lista regras ativas de um estabelecimento que têm o <paramref name="evento"/> como gatilho.
    /// Usado pelos EventHandlers de enfileiramento — caminho quente no fluxo de domain events,
    /// portanto fica em índice <c>(estabelecimento_id, evento_gatilho, ativa)</c>.
    /// </summary>
    Task<List<RegraAutomacao>> ListarAtivasPorEvento(long estabelecimentoId, string evento);

    /// <summary>Lista todas as regras de um estabelecimento (admin/CRUD), sem filtro de status.</summary>
    Task<List<RegraAutomacao>> ListarPorEstabelecimento(long estabelecimentoId);

    Task Salvar(RegraAutomacao regra);
}
