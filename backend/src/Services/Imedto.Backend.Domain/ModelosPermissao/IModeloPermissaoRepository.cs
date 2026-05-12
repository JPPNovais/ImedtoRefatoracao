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
