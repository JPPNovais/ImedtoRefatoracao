namespace Imedto.Backend.Domain.Vinculos;

public interface ISolicitacaoVinculoRepository
{
    Task<SolicitacaoVinculo> ObterPorId(long id);
    Task<SolicitacaoVinculo> ObterPorIdOuNulo(long id);

    /// <summary>
    /// Retorna a única solicitação pendente do par (profissional, estabelecimento), se existir.
    /// Usado para evitar criar duas pendentes (validação app-level — banco também tem unique parcial).
    /// </summary>
    Task<SolicitacaoVinculo> ObterPendentePorProfissionalEEstab(Guid profissionalUsuarioId, long estabelecimentoId);

    Task Salvar(SolicitacaoVinculo solicitacao);
}
