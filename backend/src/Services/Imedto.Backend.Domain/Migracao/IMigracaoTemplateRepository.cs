namespace Imedto.Backend.Domain.Migracao;

/// <summary>
/// Repositório de <see cref="MigracaoTemplate"/>.
/// Templates são cross-tenant — sem filtro por estabelecimento.
/// </summary>
public interface IMigracaoTemplateRepository
{
    Task Salvar(MigracaoTemplate template, CancellationToken ct = default);

    Task<MigracaoTemplate?> ObterPorNomeEEntidadeOuNulo(
        string nome,
        string entidade,
        CancellationToken ct = default);

    Task<List<MigracaoTemplate>> ListarPorNome(string nome, CancellationToken ct = default);

    Task<List<MigracaoTemplate>> Listar(int pagina, int tamanho, CancellationToken ct = default);

    Task<int> ContarTotal(CancellationToken ct = default);
}
