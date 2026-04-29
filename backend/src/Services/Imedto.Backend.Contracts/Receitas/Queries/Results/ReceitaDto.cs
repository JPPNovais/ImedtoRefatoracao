namespace Imedto.Backend.Contracts.Receitas.Queries.Results;

public class ReceitaResumoDto
{
    public long Id { get; set; }
    public long PacienteId { get; set; }
    public long ProntuarioId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    /// <summary>"A" | "B" | "C" | "Especial" | null (preenchido só em Controlada).</summary>
    public string? TipoNotificacao { get; set; }
    public string Status { get; set; } = string.Empty;
    /// <summary><c>null</c> em rascunho.</summary>
    public DateTime? EmitidaEm { get; set; }
    public DateTime? ValidadeAte { get; set; }
    /// <summary>
    /// Receita exige retenção pela farmácia (Portaria 344/98 — controladas;
    /// RDC 471/2021 — antibióticos). Front exibe badge "RETER" na lista.
    /// </summary>
    public bool RequerRetencao { get; set; }
    public int QuantidadeItens { get; set; }
    public string? ProfissionalNome { get; set; }
}

public class ReceitaDto
{
    public long Id { get; set; }
    public long ProntuarioId { get; set; }
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid ProfissionalUsuarioId { get; set; }
    public string? ProfissionalNome { get; set; }
    public string Tipo { get; set; } = string.Empty;
    /// <summary>"A" | "B" | "C" | "Especial" | null (preenchido só em Controlada).</summary>
    public string? TipoNotificacao { get; set; }
    public string Status { get; set; } = string.Empty;
    /// <summary><c>null</c> em rascunho.</summary>
    public DateTime? EmitidaEm { get; set; }
    public DateTime? ValidadeAte { get; set; }
    /// <summary>
    /// Receita exige retenção pela farmácia (Portaria 344/98 — controladas;
    /// RDC 471/2021 — antibióticos). Front exibe badge "RETER" no PDF/visualização.
    /// </summary>
    public bool RequerRetencao { get; set; }
    public string? Observacoes { get; set; }
    public DateTime? CanceladaEm { get; set; }
    public string? MotivoCancelamento { get; set; }
    public List<ItemReceitaDto> Itens { get; set; } = new();
}

public class ItemReceitaDto
{
    public long Id { get; set; }
    public int Ordem { get; set; }
    public string Medicamento { get; set; } = string.Empty;
    public string Posologia { get; set; } = string.Empty;
    public string? Quantidade { get; set; }
    public string? Via { get; set; }
    public string? Observacao { get; set; }
    public string? Concentracao { get; set; }
    public string? FormaFarmaceutica { get; set; }
    public string? Duracao { get; set; }
}

public class PaginaReceitasDto
{
    public IEnumerable<ReceitaResumoDto> Itens { get; set; } = Array.Empty<ReceitaResumoDto>();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanhoPagina { get; set; }
}
