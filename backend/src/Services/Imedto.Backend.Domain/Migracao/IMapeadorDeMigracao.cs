namespace Imedto.Backend.Domain.Migracao;

/// <summary>
/// Porta de mapeamento de schema por IA (briefing 2026-06-15_001 §9 — "Porta nova a definir").
///
/// Recebe apenas cabeçalhos + amostra mascarada (PII ofuscada via IAnonimizacaoService) —
/// nunca o volume real de dados (CA5, R6, D2).
/// Devolve proposta de de-para: col_origem → campo_canônico, com confiança e dúvidas.
///
/// O domínio não conhece prompt, SDK, URL de API ou provider de IA —
/// esses detalhes vivem exclusivamente no adapter de Infrastructure.
/// </summary>
public interface IMapeadorDeMigracao
{
    /// <summary>
    /// Infere o mapeamento de colunas de um arquivo para o schema canônico do Imedto.
    /// Uma chamada por arquivo — não por linha (CA23).
    /// </summary>
    /// <param name="esquema">Cabeçalhos + amostra mascarada do arquivo de origem.</param>
    /// <param name="entidadeAlvo">Entidade canônica alvo (ex: "paciente", "agendamento").</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>Proposta de mapeamento com confiança e lista de colunas com dúvida.</returns>
    Task<PropostaDeMapa> InferirMapaAsync(
        EsquemaDeArquivo esquema,
        string entidadeAlvo,
        CancellationToken ct = default);
}

/// <summary>
/// Input do mapeamento: cabeçalhos + amostra mascarada de um arquivo.
/// PII deve estar ofuscada antes de chegar aqui (CA5, R6).
/// </summary>
public sealed class EsquemaDeArquivo
{
    /// <summary>Nomes das colunas do arquivo de origem.</summary>
    public required string[] Cabecalhos { get; init; }

    /// <summary>
    /// Amostra de até N linhas com PII mascarada.
    /// Cada elemento é um dicionário col→valor_mascarado.
    /// </summary>
    public required IReadOnlyList<IReadOnlyDictionary<string, string>> AmostraMascarada { get; init; }
}

/// <summary>
/// Proposta de mapeamento devolvida pela IA.
/// Editável pelo operador no painel admin antes de ser confirmada (R7).
/// </summary>
public sealed class PropostaDeMapa
{
    /// <summary>De-para: nome da coluna de origem → nome do campo canônico no Imedto.</summary>
    public required IReadOnlyDictionary<string, string> DeParaColunas { get; init; }

    /// <summary>
    /// Confiança global do mapeamento (0.0–1.0).
    /// Abaixo de 0.6 deve ser destacado no painel como "revisão recomendada".
    /// </summary>
    public required double Confianca { get; init; }

    /// <summary>Lista de colunas com dúvida ou confiança baixa — para destacar no painel.</summary>
    public required IReadOnlyList<string> Duvidas { get; init; }
}
