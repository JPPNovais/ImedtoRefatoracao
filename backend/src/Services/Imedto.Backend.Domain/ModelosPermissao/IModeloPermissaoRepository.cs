namespace Imedto.Backend.Domain.ModelosPermissao;

public interface IModeloPermissaoRepository
{
    Task<ModeloPermissaoEstabelecimento> ObterPorId(long id);
    Task<ModeloPermissaoEstabelecimento?> ObterPorIdOuNulo(long id);
    Task<ModeloPermissaoEstabelecimento> ObterPadraoDoEstabelecimento(long estabelecimentoId);
    Task<bool> PertenceAoEstabelecimento(long modeloId, long estabelecimentoId);
    Task<bool> EstaEmUsoPorVinculoAtivo(long modeloId);

    /// <summary>
    /// Indica se já existe um modelo com o mesmo nome no estabelecimento. Usado
    /// pelos command handlers para retornar 422 antes do INSERT — sem isso, a
    /// unique constraint do DB lança DbUpdateException que vira 500 genérico.
    /// </summary>
    Task<bool> ExisteComNomeNoEstabelecimento(string nome, long estabelecimentoId, long? excetoId = null);

    Task Salvar(ModeloPermissaoEstabelecimento modelo);
    Task Excluir(ModeloPermissaoEstabelecimento modelo);

    // ─── Métodos para o contexto do Admin Global ───────────────────────────────

    /// <summary>Retorna o registro global (estabelecimento_id NULL) pelo id.</summary>
    Task<ModeloPermissaoEstabelecimento?> ObterGlobalPorIdOuNulo(long id);

    /// <summary>
    /// Retorna todos os registros globais (estabelecimento_id NULL) em ordem alfabética por nome.
    /// Usado pelo handler de criação de estabelecimento para semear as cópias padrão.
    /// </summary>
    Task<IReadOnlyList<ModeloPermissaoEstabelecimento>> ListarGlobais();

    /// <summary>Verifica se existe registro global com o mesmo nome (ignora o id informado).</summary>
    Task<bool> ExisteGlobalComNome(string nome, long? excetoId = null, CancellationToken ct = default);

    /// <summary>
    /// Verifica se algum estabelecimento já possui cópia (padrão ou não) com o mesmo nome.
    /// Usado antes de criar um padrão global — colisão bloquearia o INSERT das cópias.
    /// </summary>
    Task<bool> ExisteNomeEmQualquerEstabelecimento(string nome, long? excetoIdGlobal = null, CancellationToken ct = default);

    /// <summary>Retorna todas as cópias <c>eh_padrao=true</c> correlacionadas pelo nome (para propagação).</summary>
    Task<IReadOnlyList<ModeloPermissaoEstabelecimento>> ListarCopiasPadraoDoGlobal(string nomeGlobal, CancellationToken ct = default);

    /// <summary>
    /// Retorna true se alguma cópia eh_padrao=true com o nome informado está em uso por vínculo ativo
    /// em qualquer estabelecimento.
    /// </summary>
    Task<bool> CopiaEstaEmUsoEmQualquerEstabelecimento(string nomeGlobal, CancellationToken ct = default);

    /// <summary>Conta o total de estabelecimentos existentes (para estimativa de propagação).</summary>
    Task<int> ContarEstabelecimentos(CancellationToken ct = default);

    /// <summary>
    /// Retorna <c>true</c> se o usuário possui a permissão fina informada (catálogo em
    /// <see cref="PermissoesExtras"/>) no estabelecimento — via vínculo ativo cujo modelo
    /// tenha a chave em <c>permissoes_extras</c>, OU se for o dono do estabelecimento
    /// (dono sempre passa, replica a regra unificada de
    /// <c>IVinculoRepository.PodeAtuarComoProfissional</c>).
    /// </summary>
    Task<bool> UsuarioTemPermissaoExtra(
        Guid usuarioId,
        long estabelecimentoId,
        string permissao,
        CancellationToken ct = default);

    /// <summary>
    /// Retorna <c>true</c> se o usuário possui a ação granular informada no estabelecimento.
    /// Aceita formato legado (apenas <paramref name="area"/>, <paramref name="acao"/> = null
    /// ou vazio — qualquer ação na área concede) ou granular ("area" + "acao", precisa exatamente
    /// dessa ação OU da chave de área legada). Dono sempre passa.
    /// </summary>
    Task<bool> UsuarioTemAcao(
        Guid usuarioId,
        long estabelecimentoId,
        string area,
        string? acao = null,
        CancellationToken ct = default);
}
