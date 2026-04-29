namespace Imedto.Backend.Domain.Ia;

public interface IEstabelecimentoIaSettingsRepository
{
    /// <summary>
    /// Retorna as settings persistidas para o estabelecimento, ou <c>null</c> se nunca
    /// foi configurado (decorator deve cair em defaults globais nesse caso).
    /// </summary>
    Task<EstabelecimentoIaSettings?> ObterPorEstabelecimentoOuNulo(
        long estabelecimentoId,
        CancellationToken ct = default);

    /// <summary>
    /// Upsert: insere se ainda não existe, atualiza se já existe. PK = estabelecimento_id.
    /// </summary>
    Task Salvar(EstabelecimentoIaSettings settings, CancellationToken ct = default);
}
