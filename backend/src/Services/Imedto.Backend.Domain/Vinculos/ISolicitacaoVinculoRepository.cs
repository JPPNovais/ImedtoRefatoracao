namespace Imedto.Backend.Domain.Vinculos;

public interface ISolicitacaoVinculoRepository
{
    /// <summary>
    /// Carrega a solicitação SEM filtro de tenant — usado pelo profissional autor
    /// para cancelar a própria solicitação (caller valida ProfissionalUsuarioId).
    /// Para fluxos do dono (com tenant ativo), usar <see cref="ObterPorIdNoEstabelecimentoOuNulo"/>.
    /// </summary>
    Task<SolicitacaoVinculo?> ObterPorIdOuNulo(long id);

    /// <summary>
    /// Carrega a solicitação filtrando por <paramref name="estabelecimentoId"/>
    /// (defense-in-depth IDOR/LGPD). Retorna null se inexistente ou de outro tenant.
    /// </summary>
    Task<SolicitacaoVinculo?> ObterPorIdNoEstabelecimentoOuNulo(long id, long estabelecimentoId);

    /// <summary>
    /// Retorna a única solicitação pendente do par (profissional, estabelecimento), se existir.
    /// Usado para evitar criar duas pendentes (validação app-level — banco também tem unique parcial).
    /// </summary>
    Task<SolicitacaoVinculo> ObterPendentePorProfissionalEEstab(Guid profissionalUsuarioId, long estabelecimentoId);

    Task Salvar(SolicitacaoVinculo solicitacao);
}
