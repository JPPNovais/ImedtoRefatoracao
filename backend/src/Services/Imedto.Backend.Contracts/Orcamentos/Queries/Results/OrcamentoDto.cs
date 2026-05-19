namespace Imedto.Backend.Contracts.Orcamentos.Queries.Results;

public class OrcamentoResumoDto
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public long PacienteId { get; set; }
    public string PacienteNome { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string? Titulo { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly Validade { get; set; }
    public decimal Total { get; set; }
    public string CriadoPorNome { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public long? AgendamentoId { get; set; }
}

/// <summary>
/// Detalhe completo do orçamento (aggregate único). Carrega todas as collections —
/// itens, equipe, implantes, formas de pagamento, cirurgias — e os opcionais
/// (local cirúrgico embutido, anestesia 1:1). Não há mais distinção "resumo simples vs completo".
/// </summary>
public class OrcamentoDto : OrcamentoResumoDto
{
    public string? Observacoes { get; set; }
    public long? ProcedimentoCirurgicoId { get; set; }
    public decimal CustoImplantesTotal { get; set; }

    public List<ItemOrcamentoDto> Itens { get; set; } = new();
    public List<OrcamentoEquipeDto> Equipe { get; set; } = new();
    public List<OrcamentoImplanteDto> Implantes { get; set; } = new();
    public List<OrcamentoFormaPagamentoDto> FormasPagamento { get; set; } = new();
    public List<OrcamentoCirurgiaDto> Cirurgias { get; set; } = new();
    public OrcamentoLocalCirurgiaDto? LocalCirurgia { get; set; }
    public OrcamentoAnestesiaDto? Anestesia { get; set; }
}

public class ItemOrcamentoDto
{
    public long Id { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal DescontoPercent { get; set; }
    public decimal Subtotal { get; set; }
}

public class OrcamentoEquipeDto
{
    public long Id { get; set; }
    public Guid ProfissionalUsuarioId { get; set; }
    public string? ProfissionalNome { get; set; }
    public string Papel { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public int Ordem { get; set; }
}

public class OrcamentoImplanteDto
{
    public long Id { get; set; }
    public long? ItemInventarioId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal Quantidade { get; set; }
    public decimal CustoUnitario { get; set; }
    public decimal CustoTotal { get; set; }
}

public class OrcamentoFormaPagamentoDto
{
    public long Id { get; set; }
    public long FormaPagamentoId { get; set; }
    public string? FormaPagamentoNome { get; set; }
    public decimal Valor { get; set; }
    public int Parcelas { get; set; }
    /// <summary>Juros aplicados nesta forma de pagamento.</summary>
    public decimal AcrescimoPercentual { get; set; }
    /// <summary>% do valor da forma que vira entrada.</summary>
    public decimal EntradaPercentual { get; set; }
    public string? Observacao { get; set; }
    public int Ordem { get; set; }
}

public class OrcamentoCirurgiaDto
{
    public long Id { get; set; }
    public long? ProcedimentoCirurgicoId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public int? DuracaoMinutos { get; set; }
    public decimal ValorTotal { get; set; }
    public int Ordem { get; set; }
}

public class OrcamentoLocalCirurgiaDto
{
    /// <summary>Tipo do local cirúrgico (IntLocal/IntPeridural/IntGeral/SemInternacao/Ambulatorio).</summary>
    public string Tipo { get; set; } = string.Empty;
    /// <summary>Tempo total da cirurgia em minutos (usado no cálculo do valor).</summary>
    public int TempoMinutos { get; set; }
    /// <summary>Valor calculado/snapshot do local cirúrgico.</summary>
    public decimal Valor { get; set; }
}

public class OrcamentoAnestesiaDto
{
    public string TipoAnestesia { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string? Observacao { get; set; }
}
