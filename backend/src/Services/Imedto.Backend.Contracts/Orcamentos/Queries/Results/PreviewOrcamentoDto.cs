namespace Imedto.Backend.Contracts.Orcamentos.Queries.Results;

public class PreviewOrcamentoDto
{
    public decimal TotalCirurgias { get; set; }
    public decimal TotalEquipe { get; set; }
    public decimal TotalImplantes { get; set; }
    public decimal TotalInternacao { get; set; }
    public decimal TotalAnestesia { get; set; }
    public decimal TotalItens { get; set; }

    /// <summary>Soma de todas as collections (sem desconto/acréscimo).</summary>
    public decimal TotalGeral { get; set; }

    /// <summary>Soma dos valores das formas de pagamento (sem acréscimo).</summary>
    public decimal SomaFormas { get; set; }

    /// <summary>TotalGeral - SomaFormas. Zero quando integro.</summary>
    public decimal Diferenca { get; set; }

    /// <summary>True quando |Diferença| ≤ R$ 0,01 ou não há formas informadas.</summary>
    public bool IntegridadeOk { get; set; }

    public List<FormaPagamentoCalculadaDto> Formas { get; set; } = new();
}

public class FormaPagamentoCalculadaDto
{
    public long FormaPagamentoId { get; set; }
    public string? FormaPagamentoNome { get; set; }
    public decimal Valor { get; set; }
    public int Parcelas { get; set; }
    public decimal AcrescimoPercentual { get; set; }
    public decimal EntradaPercentual { get; set; }

    /// <summary>Valor + acréscimo aplicado.</summary>
    public decimal TotalBruto { get; set; }
    public decimal Entrada { get; set; }
    public decimal ValorParcela { get; set; }
}
