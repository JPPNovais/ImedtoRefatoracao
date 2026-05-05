namespace Imedto.Backend.Domain.Automacoes;

public interface IRegraAutomacaoRepository
{
    /// <summary>
    /// Carrega a regra filtrando por <paramref name="estabelecimentoId"/>
    /// (defense-in-depth IDOR/LGPD). Retorna null se inexistente ou de outro tenant.
    /// </summary>
    Task<RegraAutomacao?> ObterPorIdOuNulo(long id, long estabelecimentoId);

    /// <summary>
    /// Lookup sem filtro de tenant — uso restrito a operações cross-tenant
    /// legítimas (job de processamento de eventos automatizados, que não vê
    /// um tenant específico). Qualquer caller end-user (handler de request)
    /// deve usar a sobrecarga com <c>estabelecimentoId</c>.
    /// </summary>
    [Obsolete("Use ObterPorIdOuNulo(long, long) para garantir filtro de tenant. Esta sobrecarga só é permitida em jobs cross-tenant (ProcessadorAutomacoesJob).")]
    Task<RegraAutomacao?> ObterPorIdOuNuloSemTenant(long id);

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
