namespace Imedto.Backend.Contracts.Pacientes.Queries.Results;

/// <summary>
/// Resumo unificado de um documento clínico (receita, atestado ou pedido de exame).
/// Minimização LGPD: sem itens de receita, sem texto de atestado, sem lista de exames,
/// sem CID, sem diagnóstico. Conteúdo completo só é buscado ao visualizar/baixar.
/// </summary>
public class DocumentoResumoDto
{
    /// <summary>"Receita" | "Atestado" | "PedidoExame"</summary>
    public string Tipo { get; set; } = string.Empty;

    /// <summary>ID na tabela de origem.</summary>
    public long Id { get; set; }

    /// <summary>
    /// Título descritivo derivado do campo <c>tipo</c> de cada tabela.
    /// Não contém PII clínica (o tipo de receita/atestado/pedido não é diagnóstico).
    /// Ex.: "Receita Controlada", "Atestado de Afastamento", "Pedido de exame Laboratorial".
    /// </summary>
    public string Titulo { get; set; } = string.Empty;

    /// <summary>Data de emissão: emitida_em (receita) ou criado_em (atestado/pedido).</summary>
    public DateTime Data { get; set; }

    /// <summary>Nome do profissional que emitiu o documento. Pode ser nulo.</summary>
    public string? ProfissionalNome { get; set; }
}

/// <summary>
/// Página paginada de documentos unificados de um paciente.
/// </summary>
public class PaginaDocumentosDto
{
    public IEnumerable<DocumentoResumoDto> Itens { get; set; } = Enumerable.Empty<DocumentoResumoDto>();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanhoPagina { get; set; }
}
