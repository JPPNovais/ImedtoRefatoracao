namespace Imedto.Backend.Contracts.Admin.Migracao;

public sealed class MigracaoMapaDto
{
    public long Id { get; init; }
    public string Entidade { get; init; } = string.Empty;

    /// <summary>
    /// Addendum 4 (CA70/CA77) — Nome do bloco de origem no dump JSON.
    /// Ex.: "pacientes", "agendamentos". Vazio para arquivos tabulares (CSV/JSON-array).
    /// </summary>
    public string NomeBlocoOrigem { get; init; } = string.Empty;

    /// <summary>
    /// JSON com de_para, confianca, duvidas + campos do addendum 4:
    /// entidade_classificada, confianca_classificacao, ignorado, encoding_suspeito, eh_config.
    /// </summary>
    public string MapaJson { get; init; } = "{}";
    public Guid? RevisadoPorUsuarioId { get; init; }
    public DateTime? RevisadoEm { get; init; }
    public DateTime CriadoEm { get; init; }
}
