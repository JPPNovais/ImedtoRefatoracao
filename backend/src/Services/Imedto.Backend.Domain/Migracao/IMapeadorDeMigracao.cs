namespace Imedto.Backend.Domain.Migracao;

/// <summary>
/// Lista fechada de entidades canônicas suportadas pela carga (addendum 4, D-S1).
/// A IA classifica cada bloco em um desses valores ou em "sem_equivalente".
/// </summary>
public static class EntidadesCanônicas
{
    public const string Paciente             = "paciente";
    public const string Agendamento         = "agendamento";
    public const string FornecedorEstoque   = "fornecedor_estoque";
    public const string CategoriaEstoque    = "categoria_estoque";
    public const string FabricanteEstoque   = "fabricante_estoque";
    public const string LocalEstoque        = "local_estoque";
    public const string ItemEstoque         = "item_estoque";
    public const string ProdutoOrcamento    = "produto_orcamento";
    public const string ProcedimentoOrcamento = "procedimento_orcamento";
    public const string Prontuario          = "prontuario";
    public const string SemEquivalente      = "sem_equivalente";

    public static readonly IReadOnlySet<string> Todas = new HashSet<string>
    {
        Paciente, Agendamento, FornecedorEstoque, CategoriaEstoque,
        FabricanteEstoque, LocalEstoque, ItemEstoque, ProdutoOrcamento,
        ProcedimentoOrcamento, Prontuario, SemEquivalente,
    };

    public static bool EhValida(string valor) =>
        Todas.Contains(valor ?? string.Empty);
}

/// <summary>
/// Porta de mapeamento de schema por IA (addendum 4 — evolução do contrato original).
///
/// Recebe apenas cabeçalhos + amostra mascarada (PII ofuscada) —
/// nunca o volume real de dados (CA5, R6, D2 — preservados).
/// Devolve por bloco: entidade classificada + de-para de colunas numa única chamada (D-N2, R-S3, CA73, CA74).
///
/// O domínio não conhece prompt, SDK, URL de API ou provider de IA.
/// </summary>
public interface IMapeadorDeMigracao
{
    /// <summary>
    /// Infere a entidade canônica E o mapeamento de colunas de um bloco-candidato.
    /// Uma chamada por bloco — não por linha, não duas por bloco (CA74/D-N2).
    ///
    /// Para arquivos tabulares (1 bloco = arquivo inteiro), o comportamento é o mesmo.
    /// O hintNome é passado como contexto à IA (não como decisão — R-S3/CA73).
    /// </summary>
    /// <param name="esquema">Cabeçalhos + amostra mascarada do bloco de origem.</param>
    /// <param name="hintNome">Hint do nome do bloco/arquivo — contexto para a IA, não decisão.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>Proposta de classificação + mapeamento com confiança e dúvidas por bloco.</returns>
    Task<PropostaDeBlocoMapeado> InferirBlocoAsync(
        EsquemaDeArquivo esquema,
        string hintNome,
        CancellationToken ct = default);

    /// <summary>
    /// Compatibilidade: infere apenas o mapeamento de colunas (sem classificação de entidade).
    /// Mantido para não quebrar fluxos existentes que passem entidadeAlvo manualmente.
    /// </summary>
    Task<PropostaDeMapa> InferirMapaAsync(
        EsquemaDeArquivo esquema,
        string entidadeAlvo,
        CancellationToken ct = default);
}

/// <summary>
/// Input do mapeamento: cabeçalhos + amostra mascarada de um bloco.
/// PII deve estar ofuscada antes de chegar aqui (CA5, R6/R-S4).
/// </summary>
public sealed class EsquemaDeArquivo
{
    /// <summary>Nomes das colunas planas do bloco de origem.</summary>
    public required string[] Cabecalhos { get; init; }

    /// <summary>
    /// Amostra de até N linhas com PII mascarada.
    /// Cada elemento é um dicionário col→valor_mascarado.
    /// </summary>
    public required IReadOnlyList<IReadOnlyDictionary<string, string>> AmostraMascarada { get; init; }
}

/// <summary>
/// Proposta de mapeamento por bloco-candidato — inclui classificação de entidade + de-para (addendum 4).
/// Uma instância por bloco (D-N2).
/// </summary>
public sealed class PropostaDeBlocoMapeado
{
    /// <summary>
    /// Entidade canônica classificada pela IA (lista fechada D-S1).
    /// Ex.: "paciente", "agendamento", "sem_equivalente".
    /// </summary>
    public required string EntidadeClassificada { get; init; }

    /// <summary>Confiança da classificação (0.0–1.0).</summary>
    public required double ConfiancaClassificacao { get; init; }

    /// <summary>De-para: nome da coluna de origem → nome do campo canônico no Imedto.</summary>
    public required IReadOnlyDictionary<string, string> DeParaColunas { get; init; }

    /// <summary>Confiança global do mapeamento (0.0–1.0).</summary>
    public required double Confianca { get; init; }

    /// <summary>Lista de colunas com dúvida ou confiança baixa.</summary>
    public required IReadOnlyList<string> Duvidas { get; init; }
}

/// <summary>
/// Proposta de mapeamento devolvida pela IA (contrato original — mantido para compatibilidade).
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
