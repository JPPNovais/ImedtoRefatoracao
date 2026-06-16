namespace Imedto.Backend.Contracts.Admin.Migracao;

public sealed class SalvarMapaRevisadoCommand
{
    public long JobId { get; init; }

    /// <summary>
    /// Entidade canônica atual do mapa (usada para localizar o registro a revisar).
    /// Para CSS/JSON-array (legado), é a entidade detectada pelo nome.
    /// Para dump aninhado, é a entidade classificada pela IA (ou "sem_equivalente").
    /// </summary>
    public string Entidade { get; init; } = string.Empty;

    /// <summary>
    /// Addendum 4 (CA77) — Nome do bloco de origem no dump JSON.
    /// Para arquivos tabulares (CSV/JSON-array), é string.Empty (compatibilidade).
    /// </summary>
    public string NomeBlocoOrigem { get; init; } = string.Empty;

    /// <summary>
    /// Addendum 4 (CA77) — Entidade reclassificada pelo operador.
    /// Null = mantém a entidade atual (não reclassificou).
    /// Quando preenchida, substitui a entidade do mapa (o operador corrigiu a IA).
    /// </summary>
    public string? EntidadeReclassificada { get; init; }

    /// <summary>
    /// Addendum 4 (CA78) — Operador marcou o bloco como "ignorar".
    /// Quando true, o bloco não é carregado (mesmo que EntidadeReclassificada esteja preenchida).
    /// </summary>
    public bool Ignorado { get; init; }

    /// <summary>De-para revisado: coluna_origem → campo_canonico.</summary>
    public Dictionary<string, string> DePara { get; init; } = [];

    public Guid RevisadoPorUsuarioId { get; init; }
}
